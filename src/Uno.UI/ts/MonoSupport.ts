
// eslint-disable-next-line @typescript-eslint/no-namespace
namespace MonoSupport {

	/**
	 * This class is used by https://github.com/mono/mono/blob/fa726d3ac7153d87ed187abd422faa4877f85bb5/sdks/wasm/dotnet_support.js#L88 to perform
	 * unmarshaled invocation of javascript from .NET code.
	 * */
	export class jsCallDispatcher {

		private static registrations: Map<string, any> = new Map<string, any>();
		private static methodMap: { [id: string]: any } = {};
		private static _isUnoRegistered : boolean;
		private static dispatcherCallback: () => void;

		/**
		 * Registers a instance for a specified identier
		 * @param identifier the scope name
		 * @param instance the instance to use for the scope
		 */
		public static registerScope(identifier: string, instance: any) {
			jsCallDispatcher.registrations.set(identifier, instance);
		}

		public static findJSFunction(identifier: string): any {

			if (!identifier) {
				return jsCallDispatcher.dispatch;
			}
			else {
				if (!jsCallDispatcher._isUnoRegistered) {
					jsCallDispatcher.registerScope("UnoStatic_Windows_Storage_StorageFolder", Windows.Storage.StorageFolder);
					jsCallDispatcher.registerScope("UnoStatic_Windows_Storage_ApplicationDataContainer", Windows.Storage.ApplicationDataContainer);
					jsCallDispatcher.registerScope("UnoStatic_Windows_ApplicationModel_DataTransfer_DragDrop_Core_DragDropExtension", Windows.ApplicationModel.DataTransfer.DragDrop.Core.DragDropExtension);
					jsCallDispatcher.registerScope("UnoStatic_Windows_UI_Xaml_UIElement", Windows.UI.Xaml.UIElement);
					jsCallDispatcher._isUnoRegistered = true;
				}

				const { ns, methodName } = jsCallDispatcher.parseIdentifier(identifier);

				var instance = jsCallDispatcher.registrations.get(ns);

				if (instance) {
					var boundMethod = instance[methodName].bind(instance);

					var methodId = jsCallDispatcher.cacheMethod(boundMethod);

					return () => methodId;
				}
				else {
					throw `Unknown scope ${ns}`;
				}
			}
		}

		/**
		 * Internal dispatcher for methods invoked through TSInteropMarshaller
		 * @param id The method ID obtained when invoking WebAssemblyRuntime.InvokeJSUnmarshalled with a method name
		 * @param pParams The parameters structure ID
		 * @param pRet The pointer to the return value structure
		 */
		private static dispatch(id: number, pParams: any, pRet: any) {
			return jsCallDispatcher.methodMap[id + ""](pParams, pRet);
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
		private static cacheMethod(boundMethod: any): number {
			var methodId = Object.keys(jsCallDispatcher.methodMap).length;
			jsCallDispatcher.methodMap[methodId + ""] = boundMethod;
			return methodId;
		}

		private static getMethodMapId(methodHandle: number) {
			return methodHandle + "";
		}
	
		public static invokeOnMainThread() {

			if (!jsCallDispatcher.dispatcherCallback) {
				jsCallDispatcher.dispatcherCallback = (<any>globalThis).DotnetExports.UnoUIDispatching.Uno.UI.Dispatching.NativeDispatcher.DispatcherCallback;
			}

			// Use setImmediate to return avoid blocking the background thread
			// on a sync call.
			(<any>window).setImmediate(() => {
				try {
					jsCallDispatcher.dispatcherCallback();
				}
				catch (e) {
					console.error(`Unhandled dispatcher exception: ${e} (${e.stack})`);
					throw e;
				}
			});
		}
	}
}

// Export the DotNet helper for WebAssembly.JSInterop.InvokeJSUnmarshalled
(<any>window).DotNet = MonoSupport;

// Export the main thread invoker for threading support
(<any>MonoSupport).invokeOnMainThread = MonoSupport.jsCallDispatcher.invokeOnMainThread;
