namespace Windows.UI.Xaml.Media {

	export class FontFamily {

		public static async loadFont(fontFamilyName: string, fontSource: string): Promise<void> {
			// Launch the loading of the font
			const font = new FontFace(fontFamilyName, `url(${fontSource})`);

			// Wait for the font to be loaded
			await font.load();

			// TODO-AGNÈS: Handle load error to set the loader to "IsLoaded" to prevent leaking in the waiting list

			// Make it available to document
			document.fonts.add(font);

			await FontFamily.forceFontUsage(fontFamilyName);
		}

		public static async forceFontUsage(fontFamilyName: string): Promise<void> {

			// Force the browser to use it
			const dummyHiddenElement = document.createElement("p");
			dummyHiddenElement.style.fontFamily = fontFamilyName;
			dummyHiddenElement.style.opacity = "0";
			dummyHiddenElement.style.pointerEvents = "none";
			dummyHiddenElement.innerText = fontFamilyName;
			document.body.appendChild(dummyHiddenElement);

			// Yield an animation frame
			await new Promise((ok, err) => requestAnimationFrame(() => ok(null)));

			// Remove dummy element
			document.body.removeChild(dummyHiddenElement);

			// Notify font as loaded to application
			FontFamily.notifyFontLoaded(fontFamilyName);
		}

		private static managedNotifyFontLoaded?: (fontFamilyName: string) => void;

		private static notifyFontLoaded(fontFamilyName: string): void {

			if (!FontFamily.managedNotifyFontLoaded) {
				FontFamily.managedNotifyFontLoaded =
					(<any>Module).mono_bind_static_method("[Uno.UI] Windows.UI.Xaml.Media.FontFamily:NotifyFontLoaded");
			}

			FontFamily.managedNotifyFontLoaded(fontFamilyName);
		}
	}
}
