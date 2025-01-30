namespace Microsoft.UI.Xaml.Media {

	export class FontFamily {

		private static managedNotifyFontLoaded?: (fontFamilyName: string) => void;
		private static managedNotifyFontLoadFailed?: (fontFamilyName: string) => void;

		public static async loadFont(fontFamilyName: string, fontSource: string): Promise<void> {

			try {
				// Launch the loading of the font
				const font = new FontFace(fontFamilyName, `url(${fontSource})`);

				// Wait for the font to be loaded
				await font.load();

				// Make it available to document
				document.fonts.add(font);

				await FontFamily.forceFontUsage(fontFamilyName);
			}
			catch(e) {
				console.debug(`Font failed to load ${e}`);

				FontFamily.notifyFontLoadFailed(fontFamilyName);
			}
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

		private static notifyFontLoaded(fontFamilyName: string): void {

			if (!FontFamily.managedNotifyFontLoaded) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					FontFamily.managedNotifyFontLoaded = (<any>globalThis).DotnetExports.UnoUI.Microsoft.UI.Xaml.Media.FontFamilyLoader.NotifyFontLoaded;
				} else {
					throw `Unable to find dotnet exports`;
				}
			}

			FontFamily.managedNotifyFontLoaded(fontFamilyName);
		}

		private static notifyFontLoadFailed(fontFamilyName: string): void {

			if (!FontFamily.managedNotifyFontLoadFailed) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					FontFamily.managedNotifyFontLoadFailed = (<any>globalThis).DotnetExports.UnoUI.Microsoft.UI.Xaml.Media.FontFamilyLoader.NotifyFontLoadFailed;
				} else {
					throw `Unable to find dotnet exports`;
				}
			}

			FontFamily.managedNotifyFontLoadFailed(fontFamilyName);
		}
	}
}
