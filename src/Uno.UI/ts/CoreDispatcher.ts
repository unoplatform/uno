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
			MonoSupport.jsCallDispatcher.registerScope("CoreDispatcher", Windows.UI.Core.CoreDispatcher);
			CoreDispatcher.initMethods();
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
			if (Uno.UI.WindowManager.isHosted) {
				console.debug("Hosted Mode: Skipping CoreDispatcher initialization ");
			}
			else {
				if (!CoreDispatcher._coreDispatcherCallback) {
					CoreDispatcher._coreDispatcherCallback = (<any>Module).mono_bind_static_method("[Uno] Windows.UI.Core.CoreDispatcher:DispatcherCallback");
				}
			}
		}
	}
}
