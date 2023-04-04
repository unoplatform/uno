declare namespace Uno.UI.Media {
    class HtmlMediaPlayer {
        static videoWidth(htmlId: number): number;
        static videoHeight(htmlId: number): number;
        static getCurrentPosition(htmlId: number): number;
        static setCurrentPosition(htmlId: number, currentTime: number): void;
        static reload(htmlId: number): void;
        static setVolume(htmlId: number, volume: number): void;
        static getDuration(htmlId: number): number;
        static setAutoPlay(htmlId: number, enabled: boolean): void;
        static requestFullScreen(htmlId: number): void;
        static exitFullScreen(): void;
        static pause(htmlId: number): void;
        static play(htmlId: number): void;
        static stop(htmlId: number): void;
    }
}
