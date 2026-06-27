namespace Uno.UI.Dispatching {
	export class NativeDispatcher {
		static _dispatcherCallback: any;

		static _isReady: boolean;

		private static _schedule: (callback: () => void) => void = NativeDispatcher.createScheduler();

		public static init(isReady : Promise<boolean>) {

			isReady.then(() => {
				NativeDispatcher._dispatcherCallback = (<any>globalThis).DotnetExports.UnoUIDispatching.Uno.UI.Dispatching.NativeDispatcher.DispatcherCallback;

				NativeDispatcher.WakeUp(true);
				NativeDispatcher._isReady = true;
			});;
		}

		// Queues a dispatcher callback on the event loop
		public static WakeUp(force: boolean) {

			if (NativeDispatcher._isReady || force) {
				NativeDispatcher._schedule(() => {
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

		private static createScheduler(): (callback: () => void) => void {
			const scheduler = (<any>globalThis).scheduler;

			if (scheduler?.postTask instanceof Function) {
				return callback => scheduler.postTask(() => {
					try {
						callback();
					}
					catch (e) {
						window.setTimeout(() => { throw e; }, 0);
					}
				});
			}

			if (globalThis.MessageChannel instanceof Function) {
				const channel = new MessageChannel();
				const callbacks: (() => void)[] = [];

				channel.port1.onmessage = () => {
					const callback = callbacks.shift();

					if (callback) {
						callback();
					}
				};

				return callback => {
					callbacks.push(callback);
					channel.port2.postMessage(undefined);
				};
			}

			const setImmediate = (<any>window).setImmediate;

			if (setImmediate instanceof Function) {
				return callback => setImmediate(callback);
			}

			return callback => window.setTimeout(callback, 0);
		}
	}
}
