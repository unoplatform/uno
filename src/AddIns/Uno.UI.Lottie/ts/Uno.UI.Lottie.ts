declare const require: any;

namespace Uno.UI {
	export interface LottieAnimationProperties {
		elementId: string;
		jsonPath: string;
		autoplay: boolean;
		stretch: string;
		rate: number;
	}

	export interface RunningLottieAnimation {
		animation: Lottie.AnimationItem;
		properties: LottieAnimationProperties;
	}

	export class Lottie {
		private static _player: LottiePlayer;
		private static _runningAnimations: { [id: string]: RunningLottieAnimation } = {};

		public static setAnimationProperties(newProperties: LottieAnimationProperties): string {
			const elementId = newProperties.elementId;

			this.withPlayer(p => {
				let currentAnimation = this._runningAnimations[elementId];

				if (!currentAnimation || this.needNewPlayerAnimation(currentAnimation.properties, newProperties)) {
					// Here we need a new player animation
					// (some property changes required a new animation)
					currentAnimation = this.createAnimation(newProperties);
				}

				this.updateProperties(currentAnimation, newProperties);
			});

			return "ok";
		}

		public static stop(elementId: string): string {
			this.withPlayer(p => {
				this._runningAnimations[elementId].animation.stop();
			});

			return "ok";
		}

		public static play(elementId: string, looped: boolean): string {
			this.withPlayer(p => {
				const a = this._runningAnimations[elementId].animation;
				a.loop = looped;
				a.play();
			});

			return "ok";
		}

		public static kill(elementId: string): string {
			this.withPlayer(p => {
				this._runningAnimations[elementId].animation.destroy();
				delete this._runningAnimations[elementId];
			});

			return "ok";
		}

		public static pause(elementId: string): string {
			this.withPlayer(p => {
				this._runningAnimations[elementId].animation.pause();
			});

			return "ok";
		}

		public static resume(elementId: string): string {
			this.withPlayer(p => {
				this._runningAnimations[elementId].animation.play();
			});

			return "ok";
		}

		private static needNewPlayerAnimation(current: LottieAnimationProperties, newProperties: LottieAnimationProperties): boolean {

			if (newProperties.stretch != current.stretch) {
				return true;
			}
			if (newProperties.autoplay != current.autoplay) {
				return true;
			}
			if (newProperties.jsonPath != current.jsonPath) {
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

		private static createAnimation(properties: LottieAnimationProperties): RunningLottieAnimation {
			var existingAnimation = this._runningAnimations[properties.elementId];
			if (existingAnimation) {
				// destroy any previous animation
				existingAnimation.animation.destroy();
				existingAnimation.animation = null;
			}

			const config = this.getPlayerConfig(properties);
			const animation = this._player.loadAnimation(config);

			return this._runningAnimations[properties.elementId] = {
				animation: animation,
				properties: properties
			};
		}

		private static getPlayerConfig(properties: LottieAnimationProperties): Lottie.AnimationConfig {
			let scaleMode = "none";
			switch (properties.stretch) {
				case "Uniform":
					scaleMode = "xMidYMid meet";
					break;
				case "UniformToFill":
					scaleMode = "xMidYMid slice";
					break;
				case "Fill":
					scaleMode = "noScale";
					break;
			}

			// https://github.com/airbnb/lottie-web/wiki/loadAnimation-options
			const playerConfig = {
				path: properties.jsonPath,
				loop: true,
				autoplay: properties.autoplay,
				name: properties.elementId,
				renderer: "svg", // https://github.com/airbnb/lottie-web/wiki/Features
				container: document.getElementById(properties.elementId),
				rendererSettings: {
					// https://github.com/airbnb/lottie-web/wiki/Renderer-Settings
					preserveAspectRatio: scaleMode
				}
			};

			return playerConfig;
		}

		private static withPlayer(action: (player: LottiePlayer) => void): void {
			if (Lottie._player) {
				action(Lottie._player);
			} else {
				require(["lottie"], (p: LottiePlayer) => {
					if (!Lottie._player) {
						Lottie._player = p;
					}
					action(p);
				});
			}
		}
	}
}
