interface Navigator {
	wakeLock : WakeLock;
}

enum WakeLockType {
	screen = "screen"
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
