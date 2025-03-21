declare const require: unknown | undefined;
declare const config: any;

namespace Uno.UI {
	import AnimationData = Lottie.AnimationData;

	export interface LottieAnimationProperties {
		elementId: number;
		jsonPath?: string;
		autoplay: boolean;
		stretch: string;
		rate: number;
		cacheKey: string;
	}

	export interface RunningLottieAnimation {
		animation: Lottie.AnimationItem;
		properties: LottieAnimationProperties;
	}

	export class Lottie {
		private static _player: LottiePlayer;
		private static _runningAnimations: { [id: number]: RunningLottieAnimation } = {};
		private static _numberOfFrames: number;

		public static setAnimationProperties(
			newProperties: LottieAnimationProperties,
			animationData?: AnimationData): string {
			const elementId = newProperties.elementId;

			Lottie.withPlayer(p => {
				let currentAnimation = Lottie._runningAnimations[elementId];

				if (!currentAnimation || Lottie.needNewPlayerAnimation(currentAnimation.properties, newProperties)) {
					// Here we need a new player animation
					// (some property changes required a new animation)
					currentAnimation = Lottie.createAnimation(newProperties, animationData);
				}

				Lottie.updateProperties(currentAnimation, newProperties);
			});

			return "ok";
		}

		public static setAnimationPropertiesNative(
			id: number,
			path: string,
			autoplay: boolean,
			stretch: string,
			rate: number,
			cacheKey: string,
			data?: string) {

			if (data === undefined) {
				Lottie.setAnimationProperties({ elementId: id, jsonPath: path, autoplay: autoplay, stretch: stretch, rate: rate, cacheKey: cacheKey });
			} else {
				const animationData = data !== null ? <AnimationData>JSON.parse(data) : null;

				Lottie.setAnimationProperties({ elementId: id, jsonPath: path, autoplay: autoplay, stretch: stretch, rate: rate, cacheKey: cacheKey }, animationData);
			}
		}

		public static stop(elementId: number): string {
			Lottie.withPlayer(p => {
				const currentAnimation = Lottie._runningAnimations[elementId];
				if (currentAnimation) {
					const a = currentAnimation.animation;
					a.stop();
					Lottie.raiseState(a);					
				}
			});

			return "ok";
		}

		public static play(elementId: number, fromProgress: number, toProgress: number, looped: boolean): string {
			Lottie.withPlayer(p => {
				const currentAnimation = Lottie._runningAnimations[elementId];
				if (currentAnimation){
					const a = currentAnimation.animation;
					a.loop = looped;
	
					const fromFrame = fromProgress * Lottie._numberOfFrames;
					const toFrame = toProgress * Lottie._numberOfFrames;
	
					//Set forceFlag to true in order to force animation to start right away
					//Ensures calling play multiple times in quick succession plays the animation properly
					a.playSegments([fromFrame, toFrame], true);
					Lottie.raiseState(a);
				}
			});

			return "ok";
		}

		public static kill(elementId: number): string {
			Lottie.withPlayer(p => {
				const currentAnimation = Lottie._runningAnimations[elementId];
				if (currentAnimation) {
					currentAnimation.animation.destroy();
					delete Lottie._runningAnimations[elementId];
				}
			});

			return "ok";
		}

		public static pause(elementId: number): string {
			Lottie.withPlayer(p => {
				const currentAnimation = Lottie._runningAnimations[elementId];
				if (currentAnimation) {
					const a = currentAnimation.animation;
					a.pause();
					Lottie.raiseState(a);
				}
			});

			return "ok";
		}

		public static resume(elementId: number): string {
			Lottie.withPlayer(p => {
				const currentAnimation = Lottie._runningAnimations[elementId];
				if (currentAnimation) {
					const a = currentAnimation.animation;
					a.play();
					Lottie.raiseState(a);					
				}
			});

			return "ok";
		}

		public static setProgress(elementId: number, progress: number): string {
			Lottie.withPlayer(p => {
				const currentAnimation = Lottie._runningAnimations[elementId];
				if (currentAnimation) {
					const animation = currentAnimation.animation;
					let frame = Lottie._numberOfFrames * progress;
					if (frame < (animation as any).firstFrame) {
						frame = frame - (animation as any).firstFrame
					} else {
						frame = animation.getDuration(true) * progress;
					}
					animation.goToAndStop(frame, true);
					Lottie.raiseState(animation);
				}
			});

			return "ok";
		}

		public static getAnimationState(elementId: number): string {
			const animation = Lottie._runningAnimations[elementId].animation;

			const state = Lottie.getStateString(animation);

			return state;
		}

		private static needNewPlayerAnimation(current: LottieAnimationProperties, newProperties: LottieAnimationProperties): boolean {

			if (current.cacheKey !== newProperties.cacheKey) {
				return true;
			}
			if (current.jsonPath !== newProperties.jsonPath) {
				return true;
			}
			if (newProperties.stretch !== current.stretch) {
				return true;
			}
			if (newProperties.autoplay !== current.autoplay) {
				return true;
			}

			return false;
		}

		private static updateProperties(
			runningAnimation: RunningLottieAnimation,
			newProperties: LottieAnimationProperties) {

			const animation = runningAnimation.animation;
			const runningProperties = runningAnimation.properties;

			if (runningProperties == null || newProperties.rate != runningProperties.rate) {
				animation.setSpeed(newProperties.rate);
			}

			runningAnimation.properties = newProperties;
		}

		private static createAnimation(properties: LottieAnimationProperties, animationData?: AnimationData): RunningLottieAnimation {
			const existingAnimation = Lottie._runningAnimations[properties.elementId];
			if (existingAnimation) {
				// destroy any previous animation
				existingAnimation.animation.destroy();
				existingAnimation.animation = null;
			}

			const config = Lottie.getPlayerConfig(properties, animationData);
			const animation = Lottie._player.loadAnimation(config);

			const runningAnimation = {
				animation: animation,
				properties: properties
			};

			Lottie._runningAnimations[properties.elementId] = runningAnimation;

			(animation as any).addEventListener("complete", (e: any) => {
				Lottie.raiseState(animation);
			});

			(animation as any).addEventListener("loopComplete", (e: any) => {
				Lottie.raiseState(animation);
			});

			(animation as any).addEventListener("segmentStart", (e: any) => {
				Lottie.raiseState(animation);
			});

			(animation as any).addEventListener("data_ready", (e: any) => {
				Lottie._numberOfFrames = animation.totalFrames;
				Lottie.raiseState(animation);
				Lottie.raiseDataLoaded(animation);
			});

			(animation as any).addEventListener("DOMLoaded", (e: any) => {
				Lottie._numberOfFrames = animation.totalFrames;
				Lottie.raiseState(animation);
				Lottie.raiseDataLoaded(animation);
			});
			Lottie.raiseState(animation);

			return runningAnimation;
		}

		private static getStateString(animation: Lottie.AnimationItem): string {
			const duration = animation.getDuration(false);

			const state = `${animation.animationData.w}|${animation.animationData.h}|` +
				`${animation.isLoaded}|${animation.isPaused}|${duration}`;
			return state;
		}

		private static raiseState(animation: Lottie.AnimationItem) {
			const element = animation.wrapper;
			const state = Lottie.getStateString(animation);

			element.dispatchEvent(new CustomEvent("lottie_state", { detail: state }));
		}

		private static raiseDataLoaded(animation: Lottie.AnimationItem) {
			const element = animation.wrapper;
			element.dispatchEvent(new CustomEvent("animation_dom_loaded"));
		}

		private static getPlayerConfig(properties: LottieAnimationProperties, animationData?: AnimationData): Lottie.AnimationConfig {
			let scaleMode = "none";
			switch (properties.stretch) {
				case "Uniform":
					scaleMode = "xMidYMid meet";
					break;
				case "UniformToFill":
					scaleMode = "xMidYMid slice";
					break;
				case "Fill":
					scaleMode = "none";
					break;
			}

			const containerElement = (Uno.UI as any).WindowManager.current.getView(properties.elementId);

			// https://github.com/airbnb/lottie-web/wiki/loadAnimation-options
			const playerConfig: Lottie.AnimationConfig = {
				loop: true,
				autoplay: properties.autoplay,
				name: `Lottie-${properties.elementId}`,
				renderer: "svg", // https://github.com/airbnb/lottie-web/wiki/Features
				container: containerElement,
				rendererSettings: {
					// https://github.com/airbnb/lottie-web/wiki/Renderer-Settings
					preserveAspectRatio: scaleMode
				}
			};

			// Set source, with priority to animationData, if specified.
			if (animationData != null) {
				playerConfig.animationData = animationData;
			}
			else if (properties.jsonPath != null && properties.jsonPath !== "") {
				playerConfig.path = properties.jsonPath;
			}

			return playerConfig;
		}

		private static withPlayer(action: (player: LottiePlayer) => void): void {
			if (Lottie._player) {
				action(Lottie._player);
			} else {
				if (typeof require !== "function") {
					console.error("RequireJS not present.");
					return;
				}

				const dependencyToLoad = "/lottie";
				const lottieDependencyName = config.uno_dependencies.find((d: string) => d.endsWith(dependencyToLoad) || d.endsWith(dependencyToLoad + ".js"));
				require([lottieDependencyName], (p: LottiePlayer) => {
					if(!p) {
						console.error("Unable to load lottie player.");
						return;
					}
					if (!Lottie._player) {
						Lottie._player = p;
					}
					action(p);
				});
			}
		}
	}
}
