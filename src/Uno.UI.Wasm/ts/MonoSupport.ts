namespace MonoSupport {

	/**
	 * This class is used by https://github.com/mono/mono/blob/fa726d3ac7153d87ed187abd422faa4877f85bb5/sdks/wasm/dotnet_support.js#L88 to perform
	 * unmarshaled invocation of javascript from .NET code.
	 * */
	export class jsCallDispatcher {

		static registrations: Map<string, object> = new Map<string, object>();

		/**
		 * Registers a instance for a specified identier
		 * @param identifier the scope name
		 * @param instance the instance to use for the scope
		 */
		public static registerScope(identifier: string, instance: any) {
			jsCallDispatcher.registrations.set(identifier, instance);
		}

		public static findJSFunction(identifier: string): any {
			var parts = identifier.split(':');
			if (parts[0] === 'Uno') {
				var c = <any>Uno.UI.WindowManager.current;

				return c[parts[1]].bind(Uno.UI.WindowManager.current);
			}
			else {
				var instance = jsCallDispatcher.registrations.get(parts[0]);

				if (instance) {
					return (<any>instance)[parts[1]].bind(instance);
				}
				else {
					throw `Unknown scope ${parts[0]}`;
				}
			}
		}
	}
}

// Export the DotNet helper for WebAssembly.JSInterop.InvokeJSUnmarshalled
(<any>window).DotNet = MonoSupport;
