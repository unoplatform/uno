namespace Uno.Foundation.Interop {
	export class ManagedObject {
		private static assembly: UI.Interop.IMonoAssemblyHandle;
		private static dispatchMethod: UI.Interop.IMonoMethodHandle;

		private static init() {
			if (!ManagedObject.assembly) {
				ManagedObject.assembly = MonoRuntime.assembly_load("Uno.Foundation");

				if (!ManagedObject.assembly) {
					throw "Unable to find assembly Uno.Foundation";
				}
			}

			if (!ManagedObject.dispatchMethod) {
				const type = MonoRuntime.find_class(ManagedObject.assembly, "Uno.Foundation.Interop", "JSObject");
				ManagedObject.dispatchMethod = MonoRuntime.find_method(type, "Dispatch", -1);

				if (!ManagedObject.dispatchMethod) {
					throw "Unable to find Uno.Foundation.Interop.JSObject.Dispatch method";
				}
			}
		}

		public static dispatch(handle: string, method: string, parameters: string): void {
			if (!ManagedObject.dispatchMethod) {
				ManagedObject.init();
			}

            const handleStr = handle ? MonoRuntime.mono_string(handle) : null;
            const methodStr = method ? MonoRuntime.mono_string(method) : null;
            const parametersStr = parameters ? MonoRuntime.mono_string(parameters) : null;

            if (methodStr == null) {
                throw "Cannot dispatch to unknown method";
            }

			MonoRuntime.call_method(ManagedObject.dispatchMethod, null, [handleStr, methodStr, parametersStr]);
		}
	}
}
