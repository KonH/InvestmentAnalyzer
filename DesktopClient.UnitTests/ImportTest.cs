using System.IO;
using FluentAssertions;
using InvestmentAnalyzer.Importer;
using NUnit.Framework;

namespace DesktopClient.UnitTests {
	public class ImportTest {
		[Test]
		public void IsAlphaDirectStateImported() {
			using var stream = LoadFileOrIgnore("AlphaDirectState.xml");
			var result = StateImporter.LoadStateByFormat(stream, "AlphaDirectMyPortfolio");
			result.Success.Should().BeTrue(string.Join("\n", result.Errors));
		}
		
		[Test]
		public void IsTinkoffStateImported() {
			using var stream = LoadFileOrIgnore("TinkoffState.pdf");
			var result = StateImporter.LoadStateByFormat(stream, "TinkoffMyAssets");
			result.Success.Should().BeTrue(string.Join("\n", result.Errors));
		}

		[Test]
		public void IsAlphaDirectOperationsImportedOldFormat() {
			using var stream = LoadFileOrIgnore("AlphaDirectOperations_Old.xml");
			var result = OperationImporter.LoadOperationsByFormat(stream, "AlphaDirectMoneyMove");
			result.Success.Should().BeTrue(string.Join("\n", result.Errors));
		}

		[Test]
		public void IsAlphaDirectOperationsImportedNewFormat() {
			using var stream = LoadFileOrIgnore("AlphaDirectOperations_New.xml");
			var result = OperationImporter.LoadOperationsByFormat(stream, "AlphaDirectMoneyMove");
			result.Success.Should().BeTrue(string.Join("\n", result.Errors));
		}

		FileStream LoadFileOrIgnore(string fileName) {
			var currentDirectory = Directory.GetCurrentDirectory();
			var suffix = "/bin/Debug/net6.0";
			var rootDirectory = currentDirectory.Substring(0, currentDirectory.Length - suffix.Length);
			var path = Path.Combine(rootDirectory, fileName);
			var isFileExist = File.Exists(path);
			if ( !isFileExist ) {
				Assert.Ignore();
			}
			return File.OpenRead(path);
		}
	}
}