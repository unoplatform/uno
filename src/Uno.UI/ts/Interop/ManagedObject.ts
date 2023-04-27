namespace Uno.Foundation.Interop {
	export class ManagedObject {
		private static assembly: UI.Interop.IMonoAssemblyHandle;
		private static dispatchMethod: (handle: number, method: string, parameters: string) => number;

		private static init() {
			const exports = (<any>globalThis).DotnetExports?.UnoFoundationRuntimeWebAssembly?.Uno?.Foundation?.Interop?.JSObject;

			if (exports !== undefined) {
				ManagedObject.dispatchMethod = exports.Dispatch;
			} else {
				ManagedObject.dispatchMethod = (<any>Module).mono_bind_static_method("[Uno.Foundation.Runtime.WebAssembly] Uno.Foundation.Interop.JSObject:Dispatch");
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
