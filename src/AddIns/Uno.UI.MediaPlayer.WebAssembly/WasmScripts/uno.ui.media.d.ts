declare namespace Uno.UI.Media {
    class HtmlMediaPlayer {
        static videoWidth(htmlId: number): number;
        static videoHeight(htmlId: number): number;
        static getCurrentPosition(htmlId: number): number;
        static getPaused(htmlId: number): number;
        static setCurrentPosition(htmlId: number, currentTime: number): void;
        static setAttribute(htmlId: number, name: string, value: string): void;
        static removeAttribute(htmlId: number, name: string): void;
        static setPlaybackRate(htmlId: number, playbackRate: number): void;
        static reload(htmlId: number): void;
        static setVolume(htmlId: number, volume: number): void;
        static getDuration(htmlId: number): number;
        static setAutoPlay(htmlId: number, enabled: boolean): void;
        static requestFullScreen(htmlId: number): void;
        static exitFullScreen(): void;
        static requestPictureInPicture(htmlId: number): void;
        static exitPictureInPicture(): void;
        static pause(htmlId: number): void;
        static play(htmlId: number): void;
        static stop(htmlId: number): void;
    }
}
