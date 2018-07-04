namespace Uno.UI {

	export class HtmlDom {

		/**
		 * Initialize various polyfills used by Uno
		 */
		public static initPolyfills(): void {
			this.isConnectedPolyfill();
		}

		private static isConnectedPolyfill(): void {

			function get() {
				// polyfill implementation
				return document.contains(this);
			}

			(supported => {
				if (!supported) {
					Object.defineProperty(Node.prototype, "isConnected", { get });
				}

			})("isConnected" in Node.prototype);
		}
	}
}
