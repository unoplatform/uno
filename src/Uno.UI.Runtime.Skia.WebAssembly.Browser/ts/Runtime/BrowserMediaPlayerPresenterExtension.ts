namespace Uno.UI.Runtime.Skia {
	export class BrowserMediaPlayerPresenterExtension {
		static unoExports: any;

		public static buildImports() {
			BrowserMediaPlayerPresenterExtension.unoExports = WebAssemblyWindowWrapper.getAssemblyExports().Uno.UI.Runtime.Skia.BrowserMediaPlayerPresenterExtension;
		}
		
		public static getVideoNaturalHeight(elementId: string): number {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				return videoElement.videoHeight;
			}
			return 0;
		}

		public static getVideoNaturalWidth(elementId: string): number {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				return videoElement.videoWidth;
			}
			return 0;
		}

		public static requestFullscreen(elementId: string) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				videoElement.requestFullscreen();
			}
		}

		public static exitFullscreen(elementId: string) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement && document.fullscreenElement === videoElement) {
				document.exitFullscreen();
			}
		}

		public static requestPictureInPicture(elementId: string) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				// The cast is here because tsc complains about requestPictureInPicture not being present in HTMLVideoElement
				(videoElement as any).requestPictureInPicture();
			}
		}

		public static exitPictureInPicture(elementId: string) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement && document.fullscreenElement === videoElement) {
				// The cast is here because tsc complains about exitPictureInPicture not being present in Document
				(document as any).exitPictureInPicture();
			}
		}

		public static updateStretch(elementId: string, stretch: string) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				switch (stretch) {
					case "None":
						videoElement.style.objectFit = "none";
						break;
					case "Fill":
						videoElement.style.objectFit = "fill";
						break;
					case "Uniform":
						videoElement.style.objectFit = "contain";
						break;
					case "UniformToFill":
						videoElement.style.objectFit = "cover";
						break;
				}
			}
		}
		
		public static setupEvents(elementId: string) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				videoElement.onfullscreenchange = e => {
					if (!document.fullscreenElement) {
						BrowserMediaPlayerPresenterExtension.unoExports.OnExitFullscreen(elementId);
					}
				};
			}
		}
	}
}
