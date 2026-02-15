namespace Uno.UI.Runtime.Skia {

	/**
	 * Live Region Manager for screen reader announcements.
	 * Creates polite and assertive aria-live divs and manages content updates
	 * with two-tier rate limiting (debounce + sustained throttle).
	 */
	export class LiveRegion {
		private static politeRegion: HTMLDivElement;
		private static assertiveRegion: HTMLDivElement;

		/**
		 * Initializes the live region elements.
		 * Called during accessibility activation.
		 */
		public static initialize(): void {
			const container = document.getElementById("uno-body");
			if (!container) {
				return;
			}

			// Create polite live region
			LiveRegion.politeRegion = document.createElement("div");
			LiveRegion.politeRegion.id = "uno-live-region-polite";
			LiveRegion.politeRegion.setAttribute("aria-live", "polite");
			LiveRegion.politeRegion.setAttribute("aria-atomic", "true");
			LiveRegion.politeRegion.style.position = "absolute";
			LiveRegion.politeRegion.style.width = "1px";
			LiveRegion.politeRegion.style.height = "1px";
			LiveRegion.politeRegion.style.overflow = "hidden";
			LiveRegion.politeRegion.style.clip = "rect(0, 0, 0, 0)";
			LiveRegion.politeRegion.style.whiteSpace = "nowrap";
			container.appendChild(LiveRegion.politeRegion);

			// Create assertive live region
			LiveRegion.assertiveRegion = document.createElement("div");
			LiveRegion.assertiveRegion.id = "uno-live-region-assertive";
			LiveRegion.assertiveRegion.setAttribute("aria-live", "assertive");
			LiveRegion.assertiveRegion.setAttribute("aria-atomic", "true");
			LiveRegion.assertiveRegion.style.position = "absolute";
			LiveRegion.assertiveRegion.style.width = "1px";
			LiveRegion.assertiveRegion.style.height = "1px";
			LiveRegion.assertiveRegion.style.overflow = "hidden";
			LiveRegion.assertiveRegion.style.clip = "rect(0, 0, 0, 0)";
			LiveRegion.assertiveRegion.style.whiteSpace = "nowrap";
			container.appendChild(LiveRegion.assertiveRegion);
		}

		/**
		 * Updates live region content. Called from C# after rate limiting.
		 * @param handle - Element handle (unused, reserved for future per-element regions)
		 * @param content - Text content to announce
		 * @param liveSetting - 0=Off, 1=Polite, 2=Assertive
		 */
		public static updateLiveRegionContent(handle: number, content: string, liveSetting: number): void {
			const region = liveSetting === 2 ? LiveRegion.assertiveRegion : LiveRegion.politeRegion;
			if (!region) {
				return;
			}

			// Clear and re-set to trigger screen reader announcement
			region.textContent = "";
			// Use a microtask to ensure the DOM mutation triggers the announcement
			requestAnimationFrame(() => {
				region.textContent = content;
			});
		}

		/**
		 * Clears any pending content from both live regions.
		 */
		public static clearPendingAnnouncements(): void {
			if (LiveRegion.politeRegion) {
				LiveRegion.politeRegion.textContent = "";
			}
			if (LiveRegion.assertiveRegion) {
				LiveRegion.assertiveRegion.textContent = "";
			}
		}
	}
}
