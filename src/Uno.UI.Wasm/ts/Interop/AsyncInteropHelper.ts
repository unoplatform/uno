namespace Uno.UI.Interop {
	export class AsyncInteropHelper {

		private static dispatchResultMethod: (handle: number, result: string) => string;
		private static dispatchErrorMethod: (handle: number, error: string) => string;

		private static init() {
			if (AsyncInteropHelper.dispatchErrorMethod) {
				return; // already initialized
			}
			const w = window as any;
			AsyncInteropHelper.dispatchResultMethod =
				w.Module.mono_bind_static_method("[Uno.Foundation.Runtime.Wasm] Uno.Foundation.WebAssemblyRuntime:DispatchAsyncResult");
			AsyncInteropHelper.dispatchErrorMethod =
				w.Module.mono_bind_static_method("[Uno.Foundation.Runtime.Wasm] Uno.Foundation.WebAssemblyRuntime:DispatchAsyncError");
		}

		public static Invoke(handle: number, promiseFunction: () => Promise<string>): void {
			AsyncInteropHelper.init();

			try {
				promiseFunction()
					.then(str => {
						if (typeof str == "string") {
							AsyncInteropHelper.dispatchResultMethod(handle, str);
						} else {
							AsyncInteropHelper.dispatchResultMethod(handle, null);
						}
					})
					.catch(err => {
						AsyncInteropHelper.dispatchErrorMethod(handle, err);
					});
			} catch (err) {
				AsyncInteropHelper.dispatchErrorMethod(handle, err);
			}
		}
	}
}
