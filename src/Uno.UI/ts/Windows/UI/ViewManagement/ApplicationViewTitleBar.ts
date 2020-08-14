namespace Windows.UI.ViewManagement {

	export class ApplicationViewTitleBar {

		public static setBackgroundColor(colorString:string) {
			if (colorString == null) {
				//remove theme-color meta
				var metaThemeColorEntries = document.querySelectorAll("meta[name='theme-color']");
				for (let entry of metaThemeColorEntries) {
					entry.remove();
				}				
			}
			else {
				var metaThemeColorEntries = document.querySelectorAll("meta[name='theme-color']");
				var metaThemeColor: Element;
				if (metaThemeColorEntries.length == 0) {
					//create meta
					metaThemeColor = document.createElement("meta");
					metaThemeColor.setAttribute("name", "theme-color");
					document.head.appendChild(metaThemeColor);
				} else {
					metaThemeColor = metaThemeColorEntries[0];
				}
				metaThemeColor.setAttribute("content", colorString);
			}
		}
	}
}
