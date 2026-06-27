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
			const global = <any>globalThis;
			const scheduler = global.scheduler;

			if (scheduler?.postTask instanceof Function) {
				return callback => scheduler.postTask(() => {
					try {
						callback();
					}
					catch (e) {
						global.setTimeout(() => { throw e; }, 0);
					}
				});
			}

			if (global.MessageChannel instanceof Function) {
				const channel = new global.MessageChannel();
				const callbacks: { [id: number]: () => void } = {};
				let readIndex = 0;
				let writeIndex = 0;

				channel.port1.onmessage = () => {
					const callback = callbacks[readIndex];
					delete callbacks[readIndex++];

					if (readIndex === writeIndex) {
						readIndex = 0;
						writeIndex = 0;
					}

					if (callback) {
						callback();
					}
				};

				return callback => {
					callbacks[writeIndex++] = callback;
					channel.port2.postMessage(undefined);
				};
			}

			const setImmediate = global.setImmediate;

			if (setImmediate instanceof Function) {
				return callback => setImmediate(callback);
			}

			return callback => global.setTimeout(callback, 0);
		}
	}
}
