using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
			new SourceList<PortfolioStateEntry>(),
			new SourceList<PortfolioOperationEntry>(),
			new SourceList<string>(),
			new SourceList<AssetTagState>());

		public ObservableCollection<string> LogLines => _logger.Lines;

		readonly CustomLogger _logger = new();
		readonly StateRepository _repository;
		readonly ImportService _importService;
		readonly ExchangeService _exchangeService;

		AppStartup? _startup;
		AppManifest? _manifest;

		public StateManager() {
			_repository = new StateRepository(_logger);
			_importService = new ImportService(_repository);
			_exchangeService = new ExchangeService(_logger);
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
				_logger.WriteLine(e.ToString());
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
				var stateReports = LoadStateReports(broker.Name);
				foreach ( var reportPath in stateReports ) {
					await TryImportStateReport(broker, reportPath);
				}
				var operationReports = LoadOperationReports(broker.Name);
				foreach ( var reportPath in operationReports ) {
					await TryImportOperationReport(broker, reportPath);
				}
			}
			foreach ( var broker in newBrokers ) {
				State.Brokers.Add(broker);
			}
			State.Tags.AddRange(_manifest.Tags);
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
				var reportPath = $"Reports/{brokerName}/{reportName}";
				await _repository.AddEntry(path, reportPath);
				if ( await TryImportStateReport(brokerState, reportPath) ) {
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
				var reportPath = $"Reports/{brokerName}/{reportName}";
				await _repository.AddEntry(path, reportPath);
				if ( await TryImportOperationReport(brokerState, reportPath) ) {
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
					new AssetPriceMeasurement(g.Key.ToDateTime(TimeOnly.MinValue), CalculateSum(g), GetCumulativeFunds(g.Key)))
				.ToArray();

		public async Task AddTag(string tag) {
			if ( State.Tags.Items.Contains(tag) ) {
				return;
			}
			State.Tags.Add(tag);
			AssertManifest();
			_manifest.Tags.Add(tag);
			await SaveManifest();
		}

		public async Task RemoveTag(string tag) {
			State.Tags.Remove(tag);
			AssertManifest();
			_manifest.Tags.Remove(tag);
			await SaveManifest();
		}

		public void EnsureAssetTags() {
			AssertManifest();
			foreach ( var asset in State.Entries.Items ) {
				var assetTagState = State.AssetTags.Items.FirstOrDefault(a => a.Isin == asset.Isin);
				if ( assetTagState != null ) {
					continue;
				}
				assetTagState = new AssetTagState(asset.Isin, asset.Name, asset.Currency, new SourceList<string>());
				var assetTags = _manifest.AssetTags.TryGetValue(asset.Isin, out var tags)
					? tags
					: new List<string>();
				assetTagState.Tags.AddRange(assetTags);
				State.AssetTags.Add(assetTagState);
			}
		}

		public async Task AddAssetTag(string isin, string tag) {
			var assetTagState = State.AssetTags.Items.First(a => a.Isin == isin);
			assetTagState.Tags.Add(tag);
			AssertManifest();
			if ( !_manifest.AssetTags.TryGetValue(isin, out var tags) ) {
				tags = new List<string>();
				_manifest.AssetTags.Add(isin, tags);
			}
			if ( tags.Contains(tag) ) {
				return;
			}
			tags.Add(tag);
			await SaveManifest();
		}

		public async Task RemoveAssetTag(string isin, string tag) {
			var assetTagState = State.AssetTags.Items.First(a => a.Isin == isin);
			assetTagState.Tags.Remove(tag);
			AssertManifest();
			if ( _manifest.AssetTags.TryGetValue(isin, out var tags) ) {
				tags.Remove(tag);
				await SaveManifest();
			}
		}

		decimal CalculateSum(IEnumerable<PortfolioStateEntry> entries) =>
			entries.Sum(GetConvertedPrice);

		decimal GetCumulativeFunds(DateOnly date) {
			var trackTypes = new[] {
				Common.OperationType.In.ToString(),
				Common.OperationType.Out.ToString(),
			};
			return State.Operations.Items
				.Where(o => trackTypes.Contains(o.Type))
				.Where(o => o.Date <= date)
				.Sum(GetConvertedPrice);
		}

		decimal GetConvertedPrice(PortfolioStateEntry entry) =>
			GetConvertedPrice(entry.Currency, entry.TotalPrice, entry.Date);

		decimal GetConvertedPrice(PortfolioOperationEntry entry) =>
			GetConvertedPrice(entry.Currency, entry.Volume, entry.Date);

		decimal GetConvertedPrice(string currency, decimal sourcePrice, DateOnly date) {
			if ( currency == "RUB" ) {
				return sourcePrice;
			}
			var dateStr = date.ToString("dd/MM/yyyy");
			AssertManifest();
			var targetExchange = _manifest.Exchanges
				.First(e => (e.Date == dateStr) && (e.CharCode == currency));
			var targetPrice = sourcePrice * (targetExchange.Value / targetExchange.Nominal);
			return targetPrice;
		}

		async Task<bool> TryImportStateReport(BrokerState broker, string reportPath) {
			_logger.WriteLine($"Importing state '{reportPath}' for broker '{broker.Name}'");
			var result = await _importService.LoadStateByFormat(reportPath, broker.StateFormat);
			if ( !result.Success ) {
				_logger.WriteLine($"Failed to load state report '{reportPath}' for broker '{broker.Name}': {string.Join("\n", result.Errors)}");
				return false;
			}
			var date = DateOnly.FromDateTime(result.Date);
			var entries = result.Entries
				.Select(e => new PortfolioStateEntry(
					date, broker.Name, e.ISIN, e.Name, e.Currency, e.Count, e.TotalPrice, e.PricePerUnit))
				.ToList();
			foreach ( var e in entries ) {
				State.Entries.Add(e);
			}
			var portfolioState = new PortfolioState(broker.Name, date, reportPath);
			if ( !State.Periods.Items.Contains(date) ) {
				State.Periods.Add(date);
			}
			State.Portfolio.Add(portfolioState);
			_logger.WriteLine($"Import state '{reportPath}' for broker '{broker.Name}' finished");
			var requiredCurrencyCodes = entries
				.Select(e => e.Currency)
				.Where(c => c != "RUB")
				.Distinct()
				.ToArray();
			return await TryAddRequiredExchanges(date, requiredCurrencyCodes);
		}

		async Task<bool> TryImportOperationReport(BrokerState broker, string reportPath) {
			_logger.WriteLine($"Importing operations '{reportPath}' for broker '{broker.Name}'");
			var result = await _importService.LoadOperationsByFormat(reportPath, broker.OperationsFormat);
			if ( !result.Success ) {
				_logger.WriteLine($"Failed to load state report '{reportPath}' for broker '{broker.Name}': {string.Join("\n", result.Errors)}");
				return false;
			}
			var date = DateOnly.FromDateTime(result.Date);
			var operations = result.Operations
				.Where(e => e.Type is not "Ignored")
				.Select(e => new PortfolioOperationEntry(
					date, broker.Name, e.Type.ToString(), e.Currency, e.Volume))
				.ToList();
			foreach ( var e in operations ) {
				State.Operations.Add(e);
			}
			var operationState = new OperationState(broker.Name, date, reportPath);
			if ( !State.Periods.Items.Contains(date) ) {
				State.Periods.Add(date);
			}
			State.OperationStates.Add(operationState);
			_logger.WriteLine($"Import operations '{reportPath}' for broker '{broker.Name}' finished");
			var requiredCurrencyCodes = operations
				.Select(e => e.Currency)
				.Where(c => c != "RUB")
				.Distinct()
				.ToArray();
			return await TryAddRequiredExchanges(date, requiredCurrencyCodes);
		}

		async Task<bool> TryAddRequiredExchanges(DateOnly date, IReadOnlyCollection<string> requiredCurrencyCodes) {
			AssertManifest();
			if ( requiredCurrencyCodes.All(c => _manifest.Exchanges.Any(e =>
				e.Date == date.ToString("dd/MM/yyyy") && e.CharCode == c)) ) {
				return true;
			}
			var exchanges = await _exchangeService.GetExchanges(date);
			var requiredExchanges = exchanges
				.Where(e => requiredCurrencyCodes.Contains(e.CharCode))
				.Select(dto => new Exchange(dto.Date.ToString("dd/MM/yyyy"), dto.CharCode, dto.Nominal, dto.Value))
				.ToArray();
			_manifest.Exchanges.AddRange(requiredExchanges);
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

		IReadOnlyCollection<string> LoadStateReports(string brokerName) {
			AssertManifest();
			var brokerManifest = _manifest.Brokers.FirstOrDefault(b => b.Name == brokerName);
			if ( brokerManifest == null ) {
				return Array.Empty<string>();
			}
			return brokerManifest.Reports.Values;
		}

		IReadOnlyCollection<string> LoadOperationReports(string brokerName) {
			AssertManifest();
			var brokerManifest = _manifest.Brokers.FirstOrDefault(b => b.Name == brokerName);
			if ( brokerManifest == null ) {
				return Array.Empty<string>();
			}
			return brokerManifest.OperationReports.Values;
		}

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