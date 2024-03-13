namespace Windows.ApplicationModel.Core {
	/**
	 * Support file for the Windows.ApplicationModel.Core 
	 * */
	export class CoreApplication {

		public static initialize() {

			Uno.UI.Dispatching.NativeDispatcher.init(
				new Promise<boolean>(resolve => (<any>window).setImmediate(async () => {
					await CoreApplication.initializeExports();
					resolve(true);
				}))
			);
		}

		private static async initializeExports() {

			if ((<any>Module).getAssemblyExports !== undefined) {
				const unoExports = await (<any>Module).getAssemblyExports("Uno");
				const unoUIDispatchingExports = await (<any>Module).getAssemblyExports("Uno.UI.Dispatching");

				const runtimeWasmExports = await (<any>Module).getAssemblyExports("Uno.Foundation.Runtime.WebAssembly");

				if (Object.entries(unoExports).length > 0) {
					(<any>globalThis).DotnetExports = {
						Uno: unoExports,
						UnoUIDispatching: unoUIDispatchingExports,
						UnoFoundationRuntimeWebAssembly: runtimeWasmExports
					};
				}
			}
		}
	}
}
