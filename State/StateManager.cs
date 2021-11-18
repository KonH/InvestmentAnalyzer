using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InvestmentAnalyzer.Importer;
using InvestmentAnalyzer.State.Persistant;

namespace InvestmentAnalyzer.State {
	public sealed class StateManager {
		public AppState State { get; } = new(new ObservableCollection<BrokerState>());

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
					b.Key,
					b.Value.StateFormat,
					new Dictionary<DateOnly, PortfolioState>()))
				.ToArray();
			foreach ( var broker in newBrokers ) {
				broker.Portfolio.Clear();
				var reports = await LoadReports(broker.Name);
				foreach ( var (reportName, stream) in reports ) {
					var result = StateImporter.LoadStateByFormat(stream, broker.StateFormat);
					if ( !result.Success ) {
						Debug.Write($"Failed to load report '{reportName}': {string.Join("\n", result.Errors)}");
						return false;
					}
					var dateOnly = DateOnly.FromDateTime(result.Date);
					var entries = result.Entries.ToList();
					var portfolioState = new PortfolioState(dateOnly, reportName, entries);
					broker.Portfolio.Add(dateOnly, portfolioState);
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
			var targetBroker = _manifest.Brokers.GetValueOrDefault(brokerName);
			if ( targetBroker == null ) {
				return new Dictionary<string, Stream?>();
			}
			var tasks = targetBroker.Reports
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