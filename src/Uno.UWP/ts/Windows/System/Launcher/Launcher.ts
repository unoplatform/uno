
namespace Windows.System {

	export class Launcher {

		/**
		* Load the specified URL into a new tab or window
		* @param url URL to load
		* @returns "True" or "False", depending on whether a new window could be opened or not
		*/
		public open(url: string): string {
			const newWindow = window.open(url, "_blank");

			return newWindow != null
				? "True"
				: "False";
		}
	}
}
