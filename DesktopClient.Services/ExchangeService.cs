using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using InvestmentAnalyzer.State;

namespace InvestmentAnalyzer.DesktopClient.Services {
	public sealed class ExchangeService {
		public ExchangeService() {
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}

		public async Task<IReadOnlyCollection<ExchangeDto>> GetExchanges(DateOnly date) {
			var url = $"http://www.cbr.ru/scripts/XML_daily.asp?date_req={date:dd/MM/yyyy}";
			Console.WriteLine($"Read exchanges from '{url}'");
			var client = new HttpClient();
			var response = await client.GetStringAsync(url);
			var xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(response);
			var result = new List<ExchangeDto>();
			var valuleNodes = xmlDoc.SelectNodes("ValCurs/Valute");
			if ( valuleNodes == null ) {
				return result.ToArray();
			}
			foreach ( XmlNode node in valuleNodes ) {
				var charCode = node.SelectSingleNode("CharCode")?.InnerText;
				if ( string.IsNullOrEmpty(charCode) ) {
					continue;
				}
				var nominalStr = node.SelectSingleNode("Nominal")?.InnerText ?? string.Empty;
				var provider = new NumberFormatInfo {
					NumberDecimalSeparator = ","
				};
				if ( !decimal.TryParse(nominalStr, NumberStyles.Any, provider, out var nominal) ) {
					continue;
				}
				var valueStr = node.SelectSingleNode("Value")?.InnerText ?? string.Empty;
				if ( !decimal.TryParse(valueStr, NumberStyles.Any, provider, out var value) ) {
					continue;
				}
				result.Add(new ExchangeDto(date, charCode, nominal, value));
			}
			Console.WriteLine($"{result.Count} exchanges found");
			return result.ToArray();
		}
	}
}