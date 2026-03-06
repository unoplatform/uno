namespace Uno.UI.Runtime.Skia {
	export class BrowserInputHelper {
		// Read by BrowserPointerInputSource.onPointerEventReceived
		public static isBrowserZoomEnabled: boolean = true;

		public static setBrowserZoomEnabled(enabled: boolean): void {
			BrowserInputHelper.isBrowserZoomEnabled = enabled;
		}

		public static async lockKeys(keyCodes: string[]): Promise<void> {
			if ((navigator as any).keyboard) {
				await (navigator as any).keyboard.lock(
					keyCodes.length > 0 ? keyCodes : undefined
				);
			}
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
