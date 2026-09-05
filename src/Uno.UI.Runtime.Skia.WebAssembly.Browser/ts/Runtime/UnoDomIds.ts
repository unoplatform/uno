namespace Uno.UI.Runtime.Skia {

	// Single source of truth for the well-known DOM element ids of the browser head.
	// Multiple modules (window wrapper, text-box view) key behaviour off these ids, so a
	// rename must happen in exactly one place.
	export class UnoDomIds {
		public static readonly canvas = "uno-canvas";
		public static readonly input = "uno-input";
	}
}
