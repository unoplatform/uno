namespace Uno.UI.Dispatching {
	export class NativeDispatcher {
		static _dispatcherCallback: any;

		static _isReady: boolean;

		public static init(isReady : Promise<boolean>) {
			NativeDispatcher._dispatcherCallback = (<any>globalThis).DotnetExports.UnoUIDispatching.Uno.UI.Dispatching.NativeDispatcher.DispatcherCallback;

			isReady.then(() => {
				NativeDispatcher.WakeUp(true);
				NativeDispatcher._isReady = true;
			});;
		}

		// Queues a dispatcher callback on the event loop
		public static WakeUp(force: boolean) {

			if (NativeDispatcher._isReady || force) {
				(<any>window).setImmediate(() => {
					try {
						NativeDispatcher._dispatcherCallback();
					}
					catch (e) {
						console.error(`Unhandled dispatcher exception: ${e} (${e.stack})`);
						throw e;
					}
				});
			}
		}
	}
}
