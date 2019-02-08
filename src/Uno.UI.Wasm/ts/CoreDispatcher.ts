namespace Windows.UI.Core {

	/**
	 * Support file for the Windows.UI.Core 
	 * */
	export class CoreDispatcher {
		static _coreDispatcherCallback: any;
		static _isIOS: boolean;
		static _isFirstCall: boolean = true;

		public static init() {
			MonoSupport.jsCallDispatcher.registerScope("CoreDispatcher", Windows.UI.Core.CoreDispatcher);
			CoreDispatcher.initMethods();

			CoreDispatcher._isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent) && !(<any>window).MSStream;
		}

		/**
		 * Enqueues a core dispatcher callback on the javascript's event loop
		 *
		 * */
		public static WakeUp(): boolean {

			if (CoreDispatcher._isIOS && CoreDispatcher._isFirstCall) {
				//
				// This is a workaround for the available call stack during the first 5 (?) seconds
				// of the startup of an application. See https://github.com/mono/mono/issues/12357 for
				// more details.
				//
				CoreDispatcher._isFirstCall = false;
				console.debug("Detected iOS, delaying first CoreDispatched dispatch for 5s (see https://github.com/mono/mono/issues/12357)");
				window.setTimeout(() => this.WakeUp(), 5000);
			} else {
				(<any>window).setImmediate(() => CoreDispatcher._coreDispatcherCallback());

				window.setImmediate(() => {
					try {
						CoreDispatcher._coreDispatcherCallback();
					}
					catch (e) {
						console.error(`Unhandled dispatcher exception: ${e} (${e.stack})`);
						throw e;
					}
				});
			}


			return true;
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
