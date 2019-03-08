declare const require: any;
declare namespace Uno.UI {
    interface LottieAnimationProperties {
        elementId: string;
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
        static stop(elementId: string): string;
        static play(elementId: string, looped: boolean): string;
        static kill(elementId: string): string;
        static pause(elementId: string): string;
        static resume(elementId: string): string;
        private static needNewPlayerAnimation(current, newProperties);
        private static updateProperties(runningAnimation, newProperties);
        private static createAnimation(properties);
        private static getPlayerConfig(properties);
        private static withPlayer(action);
    }
}
