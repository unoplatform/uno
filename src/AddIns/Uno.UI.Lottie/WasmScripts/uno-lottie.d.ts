declare const require: any;
declare const config: any;
declare namespace Uno.UI {
    import AnimationData = Lottie.AnimationData;
    interface LottieAnimationProperties {
        elementId: number;
        jsonPath?: string;
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
        private static _numberOfFrames;
        static setAnimationProperties(newProperties: LottieAnimationProperties, animationData?: AnimationData): string;
        static stop(elementId: number): string;
        static play(elementId: number, fromProgress: number, toProgress: number, looped: boolean): string;
        static kill(elementId: number): string;
        static pause(elementId: number): string;
        static resume(elementId: number): string;
        static setProgress(elementId: number, progress: number): string;
        static getAnimationState(elementId: number): string;
        private static needNewPlayerAnimation;
        private static updateProperties;
        private static createAnimation;
        private static getStateString;
        private static raiseState;
        private static getPlayerConfig;
        private static withPlayer;
    }
}
