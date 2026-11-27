
namespace Windows.System {

	export class Launcher {

		private static readonly fileUrlLifetimeMs = 60000;

		/**
		* Load the specified URL into a new tab or window
		* @param url URL to load
		* @returns "True" or "False", depending on whether a new window could be opened or not
		*/
		public static open(url: string): string {
			const newWindow = window.open(url, "_blank");

			return newWindow != null
				? "True"
				: "False";
		}

		/**
		* Opens file content in a new tab, so the browser displays it (PDF, images, text, media)
		* or downloads it. Falls back to a plain download when the new tab is blocked.
		* @param content File content
		* @param contentType MIME type of the content
		* @param fileName Name used when the file is downloaded
		* @returns "True" once the file has been handed to the browser
		*/
		public static openFile(content: ArrayLike<number>, contentType: string, fileName: string): string {
			const blob = new Blob([new Uint8Array(content)], { type: contentType || "application/octet-stream" });
			const url = URL.createObjectURL(blob);

			// The opened tab or the download needs the URL a bit longer than this call.
			setTimeout(() => URL.revokeObjectURL(url), Launcher.fileUrlLifetimeMs);

			if (window.open(url, "_blank") != null) {
				return "True";
			}

			const anchor = document.createElement("a");
			anchor.href = url;
			anchor.download = fileName;
			anchor.style.display = "none";
			document.body.appendChild(anchor);
			anchor.click();
			document.body.removeChild(anchor);

			return "True";
		}
	}
}
