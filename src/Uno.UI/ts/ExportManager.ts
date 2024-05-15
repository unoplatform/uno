// eslint-disable-next-line @typescript-eslint/no-namespace
namespace Uno.UI {

	export class ExportManager {

		public static async initialize(): Promise<void> {

			if ((<any>Module).getAssemblyExports !== undefined) {
				const unoExports = await (<any>Module).getAssemblyExports("Uno");
				const unoUIExports = await (<any>Module).getAssemblyExports("Uno.UI");
				const unoUIDispatchingExports = await (<any>Module).getAssemblyExports("Uno.UI.Dispatching");

				const runtimeWasmExports = await (<any>Module).getAssemblyExports("Uno.Foundation.Runtime.WebAssembly");

				if (Object.entries(unoUIExports).length > 0) {
					(<any>globalThis).DotnetExports = {
						Uno: unoExports,
						UnoUI: unoUIExports,
						UnoUIDispatching: unoUIDispatchingExports,
						UnoFoundationRuntimeWebAssembly: runtimeWasmExports
					};
				}
			}
		}
	}
}
