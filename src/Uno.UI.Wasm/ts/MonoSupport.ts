namespace MonoSupport {

	/**
	 * This class is used by https://github.com/mono/mono/blob/fa726d3ac7153d87ed187abd422faa4877f85bb5/sdks/wasm/dotnet_support.js#L88 to perform
	 * unmarshaled invocation of javascript from .NET code.
	 * */
	export class jsCallDispatcher {

		static registrations: Map<string, any> = new Map<string, any>();
		static methodMap: Map<string, any> = new Map<string, any>();
		static _isUnoRegistered : boolean;

		/**
		 * Registers a instance for a specified identier
		 * @param identifier the scope name
		 * @param instance the instance to use for the scope
		 */
		public static registerScope(identifier: string, instance: any) {
			jsCallDispatcher.registrations.set(identifier, instance);
		}

		public static findJSFunction(identifier: string): any {

			if (!jsCallDispatcher._isUnoRegistered) {
				jsCallDispatcher.registerScope("UnoStatic", Uno.UI.WindowManager);
				jsCallDispatcher._isUnoRegistered = true;
			}

			var knownMethod = jsCallDispatcher.methodMap.get(identifier);
			if (knownMethod) {
				return knownMethod;
			}

			const { ns, methodName } = jsCallDispatcher.parseIdentifier(identifier);

			var instance = jsCallDispatcher.registrations.get(ns);

			if (instance) {
				var boundMethod = instance[methodName].bind(instance);

				jsCallDispatcher.cacheMethod(identifier, boundMethod);
				return boundMethod;
			}
			else {
				throw `Unknown scope ${ns}`;
			}
		}

		/**
		 * Parses the method identifier
		 * @param identifier
		 */
		private static parseIdentifier(identifier: string) {
			var parts = identifier.split(':');
			const ns = parts[0];
			const methodName = parts[1];
			return { ns, methodName };
		}

		/**
		 * Adds the a resolved method for a given identifier
		 * @param identifier the findJSFunction identifier
		 * @param boundMethod the method to call
		 */
		private static cacheMethod(identifier: string, boundMethod: any) {
			jsCallDispatcher.methodMap.set(identifier, boundMethod);
		}
	}
}

// Export the DotNet helper for WebAssembly.JSInterop.InvokeJSUnmarshalled
(<any>window).DotNet = MonoSupport;
