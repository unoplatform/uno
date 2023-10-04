namespace Windows.UI.Core {
	/**
	 * Support file for the Windows.UI.Core 
	 * */
	export class CoreDispatcher {
		static _coreDispatcherCallback: any;
		static _isFirstCall: boolean = true;
		static _isReady: Promise<boolean>;
		static _isWaitingReady: boolean;

		public static init(isReady : Promise<boolean>) {
			CoreDispatcher._isReady = isReady;
		}

		/**
		 * Enqueues a core dispatcher callback on the javascript's event loop
		 *
		 * */
		public static WakeUp(): boolean {

			// Is there a Ready promise ?
			if (CoreDispatcher._isReady) {

				// Are we already waiting for a Ready promise ?
				if (!CoreDispatcher._isWaitingReady) {
					CoreDispatcher._isReady
						.then(() => {
							CoreDispatcher.initMethods();

							CoreDispatcher.InnerWakeUp();
							CoreDispatcher._isReady = null;
						});
					CoreDispatcher._isWaitingReady = true;
				}
			}
			else {
				CoreDispatcher.InnerWakeUp();
			}

			return true;
		}

		private static InnerWakeUp() {
			(<any>window).setImmediate(() => {
				try {
					CoreDispatcher._coreDispatcherCallback();
				}
				catch (e) {
					console.error(`Unhandled dispatcher exception: ${e} (${e.stack})`);
					throw e;
				}
			});
		}

		private static initMethods() {
			if (!CoreDispatcher._coreDispatcherCallback) {
				CoreDispatcher._coreDispatcherCallback = (<any>globalThis).DotnetExports.UnoUIDispatching.Uno.UI.Dispatching.CoreDispatcher.DispatcherCallback;
			}
		}
	}
}
