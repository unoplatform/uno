namespace Uno.UI.Runtime.Skia {

	export class InputPaneExtension {
		public static blurActiveElement(): boolean {
			if (document.activeElement instanceof HTMLElement) {
				document.activeElement.blur();
				return true;
			}
			return false;
		}
	}
}
