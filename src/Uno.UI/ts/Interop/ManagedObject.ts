namespace Uno.Foundation.Interop {
	export class ManagedObject {
		private static assembly: UI.Interop.IMonoAssemblyHandle;
		private static dispatchMethod: (handle: string, method: string, parameters: string) => number;

		private static init() {
			ManagedObject.dispatchMethod = (<any>Module).mono_bind_static_method("[Uno.Foundation.Runtime.Wasm] Uno.Foundation.Interop.JSObject:Dispatch");
		}

		public static dispatch(handle: string, method: string, parameters: string): void {
			if (!ManagedObject.dispatchMethod) {
				ManagedObject.init();
			}

			ManagedObject.dispatchMethod(handle, method, parameters || "");
		}
	}
}
