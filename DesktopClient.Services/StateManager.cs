﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DesktopClient.Services;
using DynamicData;
using InvestmentAnalyzer.Importer;
using InvestmentAnalyzer.State;
using InvestmentAnalyzer.State.Persistant;

namespace InvestmentAnalyzer.DesktopClient.Services {
	public sealed class StateManager {
		public AppState State { get; } = new(
			new SourceList<BrokerState>(),
			new SourceList<DateOnly>(),
			new SourceList<PortfolioState>(),
			new SourceList<OperationState>(),
			new SourceList<PortfolioStateEntry>());

		readonly StateRepository _repository = new();
		readonly ExchangeService _exchangeService = new();

		AppStartup? _startup;
		AppManifest? _manifest;

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
				Console.WriteLine(e);
				return false;
			}
			AssertManifest();
			State.Brokers.Clear();
			var newBrokers = _manifest.Brokers
				.Select(b => new BrokerState(
					b.Name,
					b.StateFormat,
					b.OperationsFormat))
				.ToArray();
			foreach ( var broker in newBrokers ) {
				var stateReports = await LoadStateReports(broker.Name);
				foreach ( var (reportName, stream) in stateReports ) {
					await TryImportStateReport(broker, reportName, stream);
				}
				var operationReports = await LoadOperationReports(broker.Name);
				foreach ( var (reportName, stream) in operationReports ) {
					await TryImportOperationReport(broker, reportName, stream);
				}
			}
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
				StateFormat = broker.StateFormat,
				OperationsFormat = broker.OperationsFormat,
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
				if ( await TryImportStateReport(brokerState, reportName, stream) ) {
					brokerManifest.Reports.Add(reportName, reportPath);
					await SaveManifest();
				} else {
					_repository.DeleteEntry(reportPath);
				}
			}
		}

		public async Task ImportOperationPeriods(string brokerName, string[] paths) {
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
				if ( await TryImportOperationReport(brokerState, reportName, stream) ) {
					brokerManifest.OperationReports.Add(reportName, reportPath);
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

		public async Task RemoveOperationPeriod(string brokerName, DateOnly period) {
			var brokerState = State.Brokers.Items
				.FirstOrDefault(b => b.Name == brokerName);
			if ( brokerState == null ) {
				return;
			}
			AssertManifest();
			var operationPeriod = State.OperationStates.Items
				.FirstOrDefault(p => (p.BrokerName == brokerName) && (p.Date == period));
			if ( operationPeriod == null ) {
				return;
			}
			var reportName = operationPeriod.ReportName;
			var brokerManifest = _manifest.Brokers.FirstOrDefault(b => b.Name == brokerName);
			if ( brokerManifest == null ) {
				return;
			}
			if ( !brokerManifest.Reports.TryGetValue(reportName, out var reportPath) ) {
				return;
			}
			State.OperationStates.Remove(operationPeriod);
			brokerManifest.OperationReports.Remove(reportName);
			_repository.DeleteEntry(reportPath);
			await SaveManifest();
		}

		public IReadOnlyCollection<AssetPriceMeasurement> CalculateAssetPriceMeasurements() =>
			State.Entries.Items
				.GroupBy(e => e.Date)
				.Select(g =>
					new AssetPriceMeasurement(g.Key.ToDateTime(TimeOnly.MinValue), CalculateSum(g)))
				.ToArray();

		double CalculateSum(IEnumerable<PortfolioStateEntry> entries) =>
			entries.Sum(GetConvertedPrice);

		double GetConvertedPrice(PortfolioStateEntry entry) {
			if ( entry.Currency == "RUB" ) {
				return entry.TotalPrice;
			}
			var date = entry.Date.ToString("dd/MM/yyyy");
			AssertManifest();
			var sourcePrice = entry.TotalPrice;
			var targetExchange = _manifest.Exchanges
				.First(e => (e.Date == date) && (e.CharCode == entry.Currency));
			var targetPrice = sourcePrice * (targetExchange.Value / targetExchange.Nominal);
			return targetPrice;
		}

		async Task<bool> TryImportStateReport(BrokerState broker, string reportName, Stream? stream) {
			Console.WriteLine($"Importing state '{reportName}' for broker '{broker.Name}'");
			var result = StateImporter.LoadStateByFormat(stream, broker.StateFormat);
			if ( !result.Success ) {
				Console.WriteLine($"Failed to load state report '{reportName}' for broker '{broker.Name}': {string.Join("\n", result.Errors)}");
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
			if ( !State.Periods.Items.Contains(dateOnly) ) {
				State.Periods.Add(dateOnly);
			}
			State.Portfolio.Add(portfolioState);
			Console.WriteLine($"Import state '{reportName}' for broker '{broker.Name}' finished");
			var requiredCurrencyCodes = entries
				.Select(e => e.Currency)
				.Where(c => c != "RUB")
				.Distinct()
				.ToArray();
			AssertManifest();
			if ( requiredCurrencyCodes.All(c => _manifest.Exchanges.Any(e =>
				e.Date == dateOnly.ToString("dd/MM/yyyy") && e.CharCode == c)) ) {
				return true;
			}
			var exchanges = await _exchangeService.GetExchanges(dateOnly);
			var requiredExchanges = exchanges
				.Where(e => requiredCurrencyCodes.Contains(e.CharCode))
				.Select(dto => new Exchange(dto.Date.ToString("dd/MM/yyyy"), dto.CharCode, dto.Nominal, dto.Value))
				.ToArray();
			_manifest.Exchanges.AddRange(requiredExchanges);
			return await SaveManifest();
		}

		async Task<bool> TryImportOperationReport(BrokerState broker, string reportName, Stream? stream) {
			Console.WriteLine($"Importing operations '{reportName}' for broker '{broker.Name}'");
			var result = OperationImporter.LoadOperationsByFormat(stream, broker.OperationsFormat);
			if ( !result.Success ) {
				Console.WriteLine($"Failed to load state report '{reportName}' for broker '{broker.Name}': {string.Join("\n", result.Errors)}");
				return false;
			}
			// TODO: add operations
			var dateOnly = DateOnly.FromDateTime(result.Date);
			var operationState = new OperationState(broker.Name, dateOnly, reportName);
			if ( !State.Periods.Items.Contains(dateOnly) ) {
				State.Periods.Add(dateOnly);
			}
			State.OperationStates.Add(operationState);
			Console.WriteLine($"Import operations '{reportName}' for broker '{broker.Name}' finished");
			return await SaveManifest();
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

		async Task<bool> SaveManifest() {
			AssertManifest();
			return await _repository.SaveManifest(_manifest);
		}

		async Task<IReadOnlyDictionary<string, Stream?>> LoadStateReports(string brokerName) {
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

		async Task<IReadOnlyDictionary<string, Stream?>> LoadOperationReports(string brokerName) {
			AssertManifest();
			var brokerManifest = _manifest.Brokers.FirstOrDefault(b => b.Name == brokerName);
			if ( brokerManifest == null ) {
				return new Dictionary<string, Stream?>();
			}
			var tasks = brokerManifest.OperationReports
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