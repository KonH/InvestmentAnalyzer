using InvestmentAnalyzer.DesktopClient.Services;
using Splat;

namespace InvestmentAnalyzer.DesktopClient {
	static class Bootstrapper {
		public static void Register(IMutableDependencyResolver services, IReadonlyDependencyResolver resolver) {
			services.RegisterLazySingleton(() => new StateManager());
		}
	}
}