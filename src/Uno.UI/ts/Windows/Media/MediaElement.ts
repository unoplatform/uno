// @ts-nocheck
namespace Windows.Media {
	export class MediaElement {
		public static videoWidth(htmlId: number): number {
			return document.getElementById(htmlId.toString()).videoWidth;
		}

		public static videoHeight(htmlId: number): number {
			return document.getElementById(htmlId.toString()).videoHeight;
		}

		public static getCurrentPosition(htmlId: number): number {
			return document.getElementById(htmlId.toString()).currentTime;
		}

		public static setCurrentPosition(htmlId: number, currentTime: number) {
			document.getElementById(htmlId.toString()).currentTime = currentTime;
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
