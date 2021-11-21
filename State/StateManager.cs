﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using InvestmentAnalyzer.Importer;
using InvestmentAnalyzer.State.Persistant;

namespace InvestmentAnalyzer.State {
	public sealed class StateManager {
		public const string AggregationBrokerMarker = "All";

		public AppState State { get; } = new(
			new SourceList<BrokerState>(),
			new SourceList<PortfolioState>(),
			new SourceList<PortfolioStateEntry>());

		readonly StateRepository _repository;

		AppStartup? _startup;
		AppManifest? _manifest;

		public StateManager(StateRepository repository) {
			_repository = repository;
		}

		public async Task<bool> TryInitialize() {
			AssertStartup();
			return await Initialize(_startup.FilePath, false);
		}

		public async Task<bool> Initialize(string filePath, bool allowCreate) {
			AssertStartup();
			_startup.FilePath = filePath;
			_repository.FilePath = filePath;
			try {
				await LoadManifest(allowCreate);
			} catch ( Exception e ) {
				Debug.Write(e);
				return false;
			}
			AssertManifest();
			State.Brokers.Clear();
			var newBrokers = _manifest.Brokers
				.Select(b => new BrokerState(
					b.Name,
					b.StateFormat))
				.ToArray();
			foreach ( var broker in newBrokers ) {
				var reports = await LoadReports(broker.Name);
				foreach ( var (reportName, stream) in reports ) {
					TryImportReport(broker, reportName, stream);
				}
			}
			State.Brokers.Add(new BrokerState(AggregationBrokerMarker, string.Empty));
			foreach ( var broker in newBrokers ) {
				State.Brokers.Add(broker);
			}
			await SaveManifest();
			await SaveStartup();
			return true;
		}

		public async Task LoadStartup() {
			_startup = await _repository.LoadOrCreateStartup();
		}

		public async Task AddBroker(BrokerState broker) {
			State.Brokers.Add(broker);
			AssertManifest();
			_manifest.Brokers.Add(new BrokerManifest {
				Name = broker.Name,
				Reports = new Dictionary<string, string>(),
				StateFormat = broker.StateFormat
			});
			await SaveManifest();
		}

		public async Task RemoveBroker(string brokerName) {
			var brokerPortfolio = State.Portfolio.Items
				.Where(p => p.BrokerName == brokerName);
			foreach ( var period in brokerPortfolio ) {
				await RemovePortfolioPeriod(brokerName, period.Date);
			}
			var brokerState = State.Brokers.Items
				.FirstOrDefault(b => b.Name == brokerName);
			if ( brokerState == null ) {
				return;
			}
			State.Brokers.Remove(brokerState);
			AssertManifest();
			var brokerManifest = _manifest.Brokers.FirstOrDefault(b => b.Name == brokerName);
			if ( brokerManifest != null ) {
				_manifest.Brokers.Remove(brokerManifest);
			}
			await SaveManifest();
		}

		public async Task ImportPortfolioPeriods(string brokerName, string[] paths) {
			var brokerState = State.Brokers.Items
				.FirstOrDefault(b => b.Name == brokerName);
			if ( brokerState == null ) {
				return;
			}
			AssertManifest();
			var brokerManifest = _manifest.Brokers.FirstOrDefault(b => b.Name == brokerName);
			if ( brokerManifest == null ) {
				return;
			}
			foreach ( var path in paths ) {
				var reportName = Path.GetFileName(path);
				var reportPath = $"Reports/{reportName}";
				await _repository.AddEntry(path, reportPath);
				var stream = await _repository.TryLoadAsMemoryStream(reportPath);
				if ( TryImportReport(brokerState, reportName, stream) ) {
					brokerManifest.Reports.Add(reportName, reportPath);
					await SaveManifest();
				} else {
					_repository.DeleteEntry(reportPath);
				}
			}
		}

		public async Task RemovePortfolioPeriod(string brokerName, DateOnly period) {
			var brokerState = State.Brokers.Items
				.FirstOrDefault(b => b.Name == brokerName);
			if ( brokerState == null ) {
				return;
			}
			AssertManifest();
			var portfolioPeriod = State.Portfolio.Items
				.FirstOrDefault(p => (p.BrokerName == brokerName) && (p.Date == period));
			if ( portfolioPeriod == null ) {
				return;
			}
			var reportName = portfolioPeriod.ReportName;
			var brokerManifest = _manifest.Brokers.FirstOrDefault(b => b.Name == brokerName);
			if ( brokerManifest == null ) {
				return;
			}
			if ( !brokerManifest.Reports.TryGetValue(reportName, out var reportPath) ) {
				return;
			}
			State.Portfolio.Remove(portfolioPeriod);
			brokerManifest.Reports.Remove(reportName);
			_repository.DeleteEntry(reportPath);
			await SaveManifest();
		}

		bool TryImportReport(BrokerState broker, string reportName, Stream? stream) {
			Console.WriteLine($"Importing state '{reportName}' for broker '{broker.Name}'");
			var result = StateImporter.LoadStateByFormat(stream, broker.StateFormat);
			if ( !result.Success ) {
				Console.WriteLine($"Failed to load report '{reportName}' for broker '{broker.Name}': {string.Join("\n", result.Errors)}");
				return false;
			}
			var dateOnly = DateOnly.FromDateTime(result.Date);
			var entries = result.Entries
				.Select(e => new PortfolioStateEntry(
					dateOnly, broker.Name, e.ISIN, e.Name, e.Currency, e.Count, e.TotalPrice, e.PricePerUnit))
				.ToList();
			foreach ( var e in entries ) {
				State.Entries.Add(e);
			}
			var portfolioState = new PortfolioState(broker.Name, dateOnly, reportName);
			State.Portfolio.Add(portfolioState);
			Console.WriteLine($"Import state '{reportName}' for broker '{broker.Name}' finished");
			return true;
		}

		async Task SaveStartup() {
			AssertStartup();
			await _repository.SaveStartup(_startup);
		}

		async Task LoadManifest(bool allowCreate) {
			if ( allowCreate ) {
				_repository.TryCreateState();
			}
			_manifest = await _repository.LoadOrCreateManifest();
		}

		async Task SaveManifest() {
			AssertManifest();
			await _repository.SaveManifest(_manifest);
		}

		async Task<IReadOnlyDictionary<string, Stream?>> LoadReports(string brokerName) {
			AssertManifest();
			var brokerManifest = _manifest.Brokers.FirstOrDefault(b => b.Name == brokerName);
			if ( brokerManifest == null ) {
				return new Dictionary<string, Stream?>();
			}
			var tasks = brokerManifest.Reports
				.ToDictionary(
					p => p.Key,
					p => TryLoadReport(p.Value));
			await Task.WhenAll(tasks.Values);
			return tasks
				.ToDictionary(p => p.Key, p => p.Value.Result);
		}

		async Task<Stream?> TryLoadReport(string reportFilePath) =>
			await _repository.TryLoadAsMemoryStream(reportFilePath);

		[MemberNotNull(nameof(_startup))]
		void AssertStartup() {
			if ( _startup == null ) {
				throw new InvalidOperationException("No startup previously loaded");
			}
		}

		[MemberNotNull(nameof(_manifest))]
		void AssertManifest() {
			if ( _manifest == null ) {
				throw new InvalidOperationException("No state previously loaded");
			}
		}
	}
}