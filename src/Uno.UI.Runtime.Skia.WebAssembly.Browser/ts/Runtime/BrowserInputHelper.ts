namespace Uno.UI.Runtime.Skia {
	export class BrowserInputHelper {
		// Read by BrowserPointerInputSource.onPointerEventReceived
		public static isBrowserZoomEnabled: boolean = true;

		// iPadOS 13+ reports itself as a Mac, so touch support is what tells it apart from macOS Safari.
		public static readonly isIOS = /iP(ad|hone|od)/.test(navigator?.platform ?? "")
			|| (navigator?.platform === "MacIntel" && (navigator.maxTouchPoints ?? 0) > 1);

		public static setBrowserZoomEnabled(enabled: boolean): void {
			BrowserInputHelper.isBrowserZoomEnabled = enabled;
		}

		public static async lockKeys(keyCodes: string[]): Promise<void> {
			if (!BrowserInputHelper.isKeyboardLockSupported()) {
				throw new Error("Keyboard lock is not supported by this browser.");
			}

			const kb = (navigator as any).keyboard;
			await kb.lock(
				keyCodes.length > 0 ? keyCodes : undefined
			);
		}

		public static isKeyboardLockSupported(): boolean {
			const kb = (navigator as any).keyboard;
			return !!kb && typeof kb.lock === "function" && typeof kb.unlock === "function";
		}

		public static unlockKeys(): void {
			(navigator as any).keyboard?.unlock();
		}
	}
}
