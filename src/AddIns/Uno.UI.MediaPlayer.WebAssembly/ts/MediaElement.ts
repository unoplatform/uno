// @ts-nocheck
namespace Uno.UI.Media {
	export class HtmlMediaPlayer {
		public static videoWidth(htmlId: number): number {
			return document.getElementById(htmlId.toString()).videoWidth;
		}

		public static videoHeight(htmlId: number): number {
			return document.getElementById(htmlId.toString()).videoHeight;
		}

		public static getCurrentPosition(htmlId: number): number {
			const element = document.getElementById(htmlId);
			if (element !== null && element !== undefined) {
				return element.currentTime;
			} else {
				return 0;
			}
		}

		public static getPaused(htmlId: number): number {
			const element = document.getElementById(htmlId);
			if (element !== null && element !== undefined) {
				return element.paused;
			}
		}

		public static setCurrentPosition(htmlId: number, currentTime: number) {
			const element = document.getElementById(htmlId);
			if (element !== null && element !== undefined) {
				element.currentTime = currentTime;
			}
		}

		public static setAttribute(htmlId: number, name: string, value: string) {
			document.getElementById(htmlId.toString()).setAttribute(name, value);
		}
		public static removeAttribute(htmlId: number, name: string) {
			document.getElementById(htmlId.toString()).removeAttribute(name);
		}
		public static setPlaybackRate(htmlId: number, playbackRate: number) {
			document.getElementById(htmlId.toString()).playbackRate = playbackRate;
		}

		public static reload(htmlId: number) {
			var element = Uno.UI.WindowManager.current.getView(htmlId.toString());
			element.load();
		}

		public static setVolume(htmlId: number, volume: number) {
			var element = Uno.UI.WindowManager.current.getView(htmlId.toString());
			element.volume = volume;
		}

		public static getDuration(htmlId: number): number {
			return document.getElementById(htmlId.toString()).duration;
		}

		public static setAutoPlay(htmlId: number, enabled: boolean) {
			var element = Uno.UI.WindowManager.current.getView(htmlId.toString());
			element.autoplay = enabled;
		}

		public static requestFullScreen(htmlId: number) {
			var elem = Uno.UI.WindowManager.current.getView(htmlId.toString());
			var fullscreen =
				elem.requestFullscreen
				|| elem.webkitRequestFullscreen
				|| elem.mozRequestFullScreen
				|| elem.msRequestFullscreen;
			fullscreen.call(elem);
		}

		public static exitFullScreen() {
			var closeFullScreen =
				document.exitFullscreen
				|| document.mozExitFullscreen
				|| document.webkitExitFullscreen
				|| document.msExitFullscreen
			closeFullScreen.call(document);
		}

		public static requestPictureInPicture(htmlId: number) {
			var elem = Uno.UI.WindowManager.current.getView(htmlId.toString());
			if (elem !== null && document.pictureInPictureEnabled) {
				var fullscreen =
					elem.requestPictureInPicture
					|| elem.webkitRequestPictureInPicture
					|| elem.mozRequestPictureInPicture;
				fullscreen.call(elem);
			}
		}

		public static exitPictureInPicture() {
			if (document.pictureInPictureEnabled) {
				const closePictureInPicture =
					document.exitPictureInPicture
					|| document.mozCancelPictureInPicture
					|| document.webkitExitPictureInPicture
				closePictureInPicture.call(document);
			}
		}

		public static pause(htmlId: number) {
			var element = Uno.UI.WindowManager.current.getView(htmlId.toString());
			element.pause();
		}

		public static play(htmlId: number) {
			var element = Uno.UI.WindowManager.current.getView(htmlId.toString());
			element.play();
		}

		public static stop(htmlId: number) {
			var element = Uno.UI.WindowManager.current.getView(htmlId.toString());
			element.pause();
			element.currentTime = 0;
		}
	}
}
