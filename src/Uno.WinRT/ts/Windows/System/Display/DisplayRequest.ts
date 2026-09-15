interface Navigator {
	wakeLock : WakeLock;
}

enum WakeLockType {
	screen = "screen",
};

interface WakeLock {
	request(type: WakeLockType): Promise<WakeLockSentinel>;
};

interface WakeLockSentinel {    
	release(): Promise<void>;	
};

namespace Windows.System.Display {

	export class DisplayRequest {
		private static activeScreenLockPromise: Promise<WakeLockSentinel>;

		public static activateScreenLock() {
			if (navigator.wakeLock) {
				DisplayRequest.activeScreenLockPromise = navigator.wakeLock.request(WakeLockType.screen);
				DisplayRequest.activeScreenLockPromise.catch(
					reason => console.log("Could not acquire screen lock (" + reason + ")"));
			} else {
				console.log("Wake Lock API is not available in this browser.");
			}
		}

		public static deactivateScreenLock() {
			if (DisplayRequest.activeScreenLockPromise) {
				DisplayRequest.activeScreenLockPromise.then(sentinel => sentinel.release());
				DisplayRequest.activeScreenLockPromise = null;
			}
		}
	}
}
