using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using InvestmentAnalyzer.State.Persistant;

namespace InvestmentAnalyzer.State {
	public sealed class StateRepository {
		public string? FilePath { get; set; }

		string ManifestFileName = "state.json";

		public async Task<AppManifest> LoadOrCreateManifest() {
			using var zipArchive = LoadOrCreateArchive();
			var stateEntry = zipArchive.GetEntry(ManifestFileName);
			if ( stateEntry == null ) {
				return new AppManifest();
			}
			await using var manifestStream = stateEntry.Open();
			var manifest = await JsonSerializer.DeserializeAsync<AppManifest>(manifestStream);
			return manifest ?? new AppManifest();
		}

		public async Task SaveManifest(AppManifest manifest) {
			using var zipArchive = LoadOrCreateArchive();
			var stateEntry = zipArchive.GetEntry(ManifestFileName) ?? zipArchive.CreateEntry(ManifestFileName);
			await using var manifestStream = stateEntry.Open();
			manifestStream.SetLength(0);
			await JsonSerializer.SerializeAsync(manifestStream, manifest);
		}

		public async Task<Stream?> TryLoadAsMemoryStream(string entryName) {
			using var zipArchive = LoadOrCreateArchive();
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

		ZipArchive LoadOrCreateArchive() {
			if ( string.IsNullOrEmpty(FilePath) ) {
				throw new InvalidOperationException($"{nameof(FilePath)} is not set");
			}
			var stream = File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
			return new ZipArchive(stream, ZipArchiveMode.Update);
		}
	}
}