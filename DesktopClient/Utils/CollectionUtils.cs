using System.Collections.Generic;
using DynamicData;

namespace InvestmentAnalyzer.DesktopClient.Utils {
	public static class CollectionUtils {
		public static void ReplaceWithRange<T>(this IList<T> source, IEnumerable<T>? target) {
			source.Clear();
			if ( target != null ) {
				source.AddRange(target);
			}
		}
	}
}