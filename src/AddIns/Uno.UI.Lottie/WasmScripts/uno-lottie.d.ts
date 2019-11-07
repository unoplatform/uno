declare const require: any;
declare namespace Uno.UI {
    interface LottieAnimationProperties {
        elementId: number;
        jsonPath: string;
        autoplay: boolean;
        stretch: string;
        rate: number;
    }
    interface RunningLottieAnimation {
        animation: Lottie.AnimationItem;
        properties: LottieAnimationProperties;
    }
    class Lottie {
        private static _player;
        private static _runningAnimations;
        static setAnimationProperties(newProperties: LottieAnimationProperties): string;
        static stop(elementId: number): string;
        static play(elementId: number, looped: boolean): string;
        static kill(elementId: number): string;
        static pause(elementId: number): string;
        static resume(elementId: number): string;
        static setProgress(elementId: number, progress: number): string;
        static getAnimationState(elementId: number): string;
        private static needNewPlayerAnimation;
        private static updateProperties;
        private static createAnimation;
        private static raiseState;
        private static getPlayerConfig;
        private static withPlayer;
    }
}
