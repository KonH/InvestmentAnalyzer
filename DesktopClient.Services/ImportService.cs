using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using InvestmentAnalyzer.Importer;
using InvestmentAnalyzer.State;
using State.Persistant;

namespace DesktopClient.Services {
	public sealed class ImportService {
		const string CacheFileName = "cache.json";

		readonly StateRepository _repository;

		Cache? _cache;

		public ImportService(StateRepository repository) {
			_repository = repository;
		}

		public async Task<StateImporter.ImportResult> LoadStateByFormat(string reportPath, string stateFormat) {
			await TryRefreshCache();
			if ( _cache.States.TryGetValue(reportPath, out var result) ) {
				return result;
			}
			var stream = await TryLoadReport(reportPath);
			result = StateImporter.LoadStateByFormat(stream, stateFormat);
			_cache.States.Add(reportPath, result);
			await SaveCache();
			return result;
		}

		public async Task<OperationImporter.ImportResult> LoadOperationsByFormat(string reportPath, string operationsFormat) {
			await TryRefreshCache();
			if ( _cache.Operations.TryGetValue(reportPath, out var result) ) {
				return result;
			}
			var stream = await TryLoadReport(reportPath);
			result = OperationImporter.LoadOperationsByFormat(stream, operationsFormat);
			_cache.Operations.Add(reportPath, result);
			await SaveCache();
			return result;
		}

		async Task<Stream?> TryLoadReport(string reportFilePath) =>
			await _repository.TryLoadAsMemoryStream(reportFilePath);

#pragma warning disable CS8774 // Strange behaviour
		[MemberNotNull(nameof(_cache))]
		async Task TryRefreshCache() {
			if ( _cache != null ) {
				return;
			}
			var cache = await _repository.LoadOrCreate<Cache>(CacheFileName);
			_cache = cache ?? throw new InvalidOperationException();
		}
#pragma warning restore CS8774

		async Task SaveCache() {
			await TryRefreshCache();
			await _repository.Save(CacheFileName, _cache);
		}
	}
}