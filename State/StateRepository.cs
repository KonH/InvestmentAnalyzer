using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using InvestmentAnalyzer.State.Persistant;

namespace InvestmentAnalyzer.State {
	public sealed class StateRepository {
		public string? FilePath { get; set; }

		string ManifestFileName = "state.json";

		string StartupPath =>
			Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) ?? string.Empty, "DesktopClient.Startup.json");

		readonly CustomLogger _logger;

		public StateRepository(CustomLogger logger) {
			_logger = logger;
		}

		public void TryCreateState() {
			if ( File.Exists(FilePath) ) {
				return;
			}
			var archive = LoadOrCreateArchive();
			archive.Dispose();
		}

		public async Task<AppStartup> LoadOrCreateStartup() {
			var path = StartupPath;
			if ( !File.Exists(path) ) {
				return new AppStartup();
			}
			await using var stream = File.Open(path, FileMode.Open);
			return await JsonSerializer.DeserializeAsync<AppStartup>(stream) ?? new AppStartup();
		}

		public async Task SaveStartup(AppStartup startup) {
			await using var stream = File.Open(StartupPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
			stream.SetLength(0);
			await JsonSerializer.SerializeAsync(stream, startup);
		}

		public async Task<AppManifest> LoadOrCreateManifest() {
			using var zipArchive = LoadArchive();
			var stateEntry = zipArchive.GetEntry(ManifestFileName);
			if ( stateEntry == null ) {
				return new AppManifest();
			}
			await using var manifestStream = stateEntry.Open();
			var manifest = await JsonSerializer.DeserializeAsync<AppManifest>(manifestStream);
			return manifest ?? new AppManifest();
		}

		public async Task<bool> SaveManifest(AppManifest manifest) {
			using var zipArchive = LoadArchive();
			var stateEntry = zipArchive.GetEntry(ManifestFileName) ?? zipArchive.CreateEntry(ManifestFileName);
			await using var manifestStream = stateEntry.Open();
			manifestStream.SetLength(0);
			try {
				await JsonSerializer.SerializeAsync(manifestStream, manifest);
				return true;
			} catch ( Exception e ) {
				_logger.WriteLine(e.ToString());
				return false;
			}
		}

		public async Task<Stream?> TryLoadAsMemoryStream(string entryName) {
			using var zipArchive = LoadArchive();
			var entry = zipArchive.GetEntry(entryName);
			if ( entry == null ) {
				return null;
			}
			await using var sourceStream = entry.Open();
			var memoryStream = new MemoryStream();
			await sourceStream.CopyToAsync(memoryStream);
			memoryStream.Position = 0;
			return memoryStream;
		}

		public async Task AddEntry(string sourcePath, string entryName) {
			await using var sourceStream = File.OpenRead(sourcePath);
			using var zipArchive = LoadArchive();
			var entry = zipArchive.CreateEntry(entryName);
			await using var targetStream = entry.Open();
			await sourceStream.CopyToAsync(targetStream);
		}

		public void DeleteEntry(string entryName) {
			using var zipArchive = LoadArchive();
			var entry = zipArchive.GetEntry(entryName);
			entry?.Delete();
		}

		ZipArchive LoadArchive() {
			if ( !File.Exists(FilePath) ) {
				throw new FileNotFoundException($"File not found at '{FilePath}'");
			}
			return LoadOrCreateArchive();
		}

		ZipArchive LoadOrCreateArchive() {
			if ( string.IsNullOrEmpty(FilePath) ) {
				throw new InvalidOperationException($"{nameof(FilePath)} is not set");
			}
			var stream = File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
			return new ZipArchive(stream, ZipArchiveMode.Update);
		}
	}
}