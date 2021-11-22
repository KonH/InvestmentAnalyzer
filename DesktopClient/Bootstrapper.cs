using InvestmentAnalyzer.State;
using Splat;

namespace InvestmentAnalyzer.DesktopClient {
	static class Bootstrapper {
		public static void Register(IMutableDependencyResolver services, IReadonlyDependencyResolver resolver) {
			services.RegisterLazySingleton(() => new StateRepository());
			services.RegisterLazySingleton(() => new StateManager(resolver.GetService<StateRepository>()));
		}
	}
}