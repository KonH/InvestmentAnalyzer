using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InvestmentAnalyzer.State {
	public sealed class StateManager {
		readonly StateRepository _repository;

		AppManifest? _manifest;

		public StateManager(StateRepository repository) {
			_repository = repository;
		}

		public async Task Load() {
			_manifest = await _repository.LoadOrCreateManifest();
		}

		public async Task Save() {
			AssertManifest();
			await _repository.SaveManifest(_manifest);
		}

		public async Task<IReadOnlyDictionary<string, Stream?>> LoadReports(string brokerName) {
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