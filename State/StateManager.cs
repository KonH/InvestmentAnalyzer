using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

		AppManifest? _manifest;

		public StateManager(StateRepository repository) {
			_repository = repository;
		}

		public async Task Initialize(string filePath) {
			_repository.FilePath = filePath;
			await Load();
			AssertManifest();
			State.Brokers.Clear();
			var newBrokers = _manifest.Brokers
				.Select(b => new BrokerState(
					b.Key,
					b.Value.StateFormat,
					new Dictionary<DateOnly, PortfolioState>()));
			foreach ( var broker in newBrokers ) {
				State.Brokers.Add(broker);
			}
			foreach ( var broker in State.Brokers ) {
				broker.Portfolio.Clear();
				var reports = await LoadReports(broker.Name);
				foreach ( var (reportName, stream) in reports ) {
					var result = StateImporter.LoadStateByFormat(stream, broker.StateFormat);
					if ( !result.Success ) {
						throw new InvalidOperationException(
							$"Failed to load report '{reportName}': {string.Join("\n", result.Errors)}");
					}
					var dateOnly = DateOnly.FromDateTime(result.Date);
					var entries = result.Entries.ToList();
					var portfolioState = new PortfolioState(dateOnly, reportName, entries);
					broker.Portfolio.Add(dateOnly, portfolioState);
				}
			}
			await Save();
		}

		async Task Load() {
			_manifest = await _repository.LoadOrCreateManifest();
		}

		async Task Save() {
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

		[MemberNotNull(nameof(_manifest))]
		void AssertManifest() {
			if ( _manifest == null ) {
				throw new InvalidOperationException("No state previously loaded");
			}
		}
	}
}