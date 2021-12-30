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
		public AppState State { get; } = new();

		public ObservableCollection<string> LogLines => _logger.Lines;

		readonly CustomLogger _logger = new();
		readonly StateRepository _repository;
		readonly ImportService _importService;
		readonly ExchangeService _exchangeService;

		AppStartup? _startup;
		AppManifest? _manifest;

		public StateManager() {
			_repository = new(_logger);
			_importService = new(_repository);
			_exchangeService = new(_logger);
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
			foreach ( var (group, groupEntries) in _manifest.Groups ) {
				State.Groups.Add(new(group));
				var entries = groupEntries
					.Select(p => new GroupStateEntry(group, p.Key, p.Value));
				State.GroupEntries.AddRange(entries);
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

		public async Task AddNewGroup(string name) {
			if ( State.Groups.Items.Any(g => g.Name == name) ) {
				return;
			}
			State.Groups.Add(new(name));
			AssertManifest();
			_manifest.Groups.Add(name, new());
			await SaveManifest();
		}

		public async Task RemoveGroup(string name) {
			var groups = State.Groups.Items.Where(g => g.Name == name);
			State.Groups.RemoveMany(groups);
			var entries = State.GroupEntries.Items.Where(e => e.Group == name);
			State.GroupEntries.RemoveMany(entries);
			AssertManifest();
			_manifest.Groups.Remove(name);
			await SaveManifest();
		}

		public async Task AddNewGroupEntry(string group, string tag, decimal target) {
			if ( State.GroupEntries.Items.Any(e => IsTargetGroupEntry(e, group, tag)) ) {
				return;
			}
			State.GroupEntries.Add(new(group, tag, target));
			AssertManifest();
			if ( _manifest.Groups.TryGetValue(group, out var groupEntries) ) {
				groupEntries.Add(tag, target);
				await SaveManifest();
			}
		}

		public async Task UpdateGroupEntry(string group, string tag, decimal target) {
			var entries = State.GroupEntries.Items.Where(e => IsTargetGroupEntry(e, group, tag));
			foreach ( var entry in entries ) {
				entry.Target = target;
			}
			AssertManifest();
			if ( _manifest.Groups.TryGetValue(group, out var groupEntries) ) {
				groupEntries[tag] = target;
				await SaveManifest();
			}
		}

		public async Task RemoveGroupEntry(string group, string tag) {
			var entries = State.GroupEntries.Items.Where(e => IsTargetGroupEntry(e, group, tag));
			State.GroupEntries.RemoveMany(entries);
			AssertManifest();
			if ( _manifest.Groups.TryGetValue(group, out var groupEntries) ) {
				groupEntries.Remove(tag);
				await SaveManifest();
			}
		}

		bool IsTargetGroupEntry(GroupStateEntry entry, string group, string tag) =>
			entry.Group == group && entry.Tag == tag;

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

		decimal GetConvertedPrice(IReadOnlyCollection<PortfolioStateEntry> entries) =>
			entries
				.Sum(e => GetConvertedPrice(e.Currency, e.TotalPrice, e.Date));

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

		public (IReadOnlyCollection<(PortfolioStateEntry, decimal)>, decimal) GetLatestPortfolio() {
			var brokers = State.Brokers.Items
				.Select(b => b.Name)
				.ToArray();
			var latestAssets = brokers
				.Select(b => {
					var bp = State.Portfolio.Items
						.Where(s => s.BrokerName == b)
						.OrderByDescending(s => s.Date)
						.FirstOrDefault();
					if ( bp != null ) {
						return State.Entries.Items
							.Where(e => e.BrokerName == b && e.Date == bp.Date)
							.ToArray();
					}
					return Array.Empty<PortfolioStateEntry>();
				})
				.SelectMany(e => e)
				.ToArray();
			var totalPrice = GetConvertedPrice(latestAssets);
			var assetsWithPrice = latestAssets
				.Select(a => (a, GetConvertedPrice(a)))
				.ToArray();
			return (assetsWithPrice, totalPrice);
		}

		public IReadOnlyCollection<(PortfolioStateEntry, decimal)> GetAssetsForTag(
			IReadOnlyCollection<(PortfolioStateEntry, decimal)> assets, string tag) =>
			assets
				.Where(p => {
					var (e, _) = p;
					return State.AssetTags.Items.Any(t => t.Isin == e.Isin && t.Tags.Items.Contains(tag));
				})
				.ToArray();

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