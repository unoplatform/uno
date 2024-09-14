namespace Uno.Foundation.Interop {
	export class ManagedObject {
		private static assembly: UI.Interop.IMonoAssemblyHandle;
		private static dispatchMethod: (handle: number, method: string, parameters: string) => number;

		private static init() {
			const exports = (<any>globalThis).DotnetExports?.UnoFoundationRuntimeWebAssembly?.Uno?.Foundation?.Interop?.JSObject;

			if (exports !== undefined) {
				ManagedObject.dispatchMethod = exports.Dispatch;
			} else {
				throw `Unable to find dotnet exports`;
			}
		}

		public static dispatch(handle: number, method: string, parameters: string): void {
			if (!ManagedObject.dispatchMethod) {
				ManagedObject.init();
			}

			ManagedObject.dispatchMethod(handle, method, parameters || "");
		}
	}
}
