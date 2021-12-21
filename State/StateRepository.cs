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

		public async Task<AppManifest> LoadOrCreateManifest() =>
			await LoadOrCreate<AppManifest>(ManifestFileName);

		public async Task<T> LoadOrCreate<T>(string path) where T : class, new() {
			using var zipArchive = LoadArchive();
			var stateEntry = zipArchive.GetEntry(path);
			if ( stateEntry == null ) {
				return new T();
			}
			await using var stream = stateEntry.Open();
			try {
				var obj = await JsonSerializer.DeserializeAsync<T>(stream);
				return obj ?? new T();
			} catch ( Exception e ) {
				_logger.WriteLine(e.ToString());
				return new T();
			}
		}

		public async Task<bool> SaveManifest(AppManifest manifest) =>
			await Save(ManifestFileName, manifest);

		public async Task<bool> Save<T>(string path, T obj) where T : class {
			using var zipArchive = LoadArchive();
			var entry = zipArchive.GetEntry(path) ?? zipArchive.CreateEntry(path);
			await using var stream = entry.Open();
			stream.SetLength(0);
			try {
				var options = new JsonSerializerOptions {
					WriteIndented = true
				};
				await JsonSerializer.SerializeAsync(stream, obj, options);
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