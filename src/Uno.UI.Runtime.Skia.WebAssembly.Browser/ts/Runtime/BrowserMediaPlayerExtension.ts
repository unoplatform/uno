namespace Uno.UI.Runtime.Skia {
	export class BrowserMediaPlayerExtension {
		static unoExports: any;

		public static buildImports() {
			BrowserMediaPlayerExtension.unoExports = WebAssemblyWindowWrapper.getAssemblyExports().Uno.UI.Runtime.Skia.BrowserMediaPlayerExtension;
		}
		
		public static getVideoPlaybackRate(elementId: string): number {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				return videoElement.playbackRate;
			}
			return 1;
		}

		public static setVideoPlaybackRate(elementId: string, playbackRate: number) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				videoElement.playbackRate = playbackRate;
				videoElement.buffered
			}
		}

		public static getIsVideoLooped(elementId: string): boolean {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				return videoElement.loop;
			}
			return false;
		}

		public static setIsVideoLooped(elementId: string, isLooped: boolean) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				videoElement.loop = isLooped;
			}
		}

		public static getDuration(elementId: string): number {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				return videoElement.duration;
			}
			return 0;
		}

		public static setSource(elementId: string, uri: string) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				videoElement.src = uri;
				videoElement.load();
			}
		}

		public static play(elementId: string) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				videoElement.play();
			}
		}

		public static pause(elementId: string) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				videoElement.pause();
			}
		}

		public static stop(elementId: string) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				videoElement.load(); // this is not a typo, loading an already-loaded video resets (i.e. stops) it.
			}
		}

		public static getPosition(elementId: string): number {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				return videoElement.currentTime;
			}
			return 0;
		}

		public static setPosition(elementId: string, position: number) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				videoElement.currentTime = position;
			}
		}

		public static setMuted(elementId: string, muted: boolean) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				videoElement.muted = muted;
			}
		}

		public static setVolume(elementId: string, volume: number) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				videoElement.volume = volume;
			}
		}

		public static setupEvents(elementId: string) {
			const videoElement = document.getElementById(elementId) as HTMLVideoElement;
			if (videoElement) {
				videoElement.onloadedmetadata = e => BrowserMediaPlayerExtension.unoExports.OnLoadedMetadata(elementId, videoElement.videoWidth !== 0);
				videoElement.onstalled = e => BrowserMediaPlayerExtension.unoExports.OnStalled(elementId);
				videoElement.onratechange = e => BrowserMediaPlayerExtension.unoExports.OnRateChange(elementId);
				videoElement.ondurationchange = e => BrowserMediaPlayerExtension.unoExports.OnDurationChange(elementId);
				videoElement.onended = e => BrowserMediaPlayerExtension.unoExports.OnEnded(elementId);
				videoElement.onerror = e => BrowserMediaPlayerExtension.unoExports.OnError(elementId);
				videoElement.onpause = e => BrowserMediaPlayerExtension.unoExports.OnPause(elementId);
				videoElement.onplaying = e => BrowserMediaPlayerExtension.unoExports.OnPlaying(elementId);
				videoElement.onseeked = e => BrowserMediaPlayerExtension.unoExports.OnSeeked(elementId);
				videoElement.onvolumechange = e => BrowserMediaPlayerExtension.unoExports.OnVolumeChange(elementId);
				// videoElement.ontimeupdate = e => BrowserMediaPlayerExtension.unoExports.OnTimeUpdate(elementId);
			}
		}
	}
}
