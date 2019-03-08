var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        class Lottie {
            static setAnimationProperties(newProperties) {
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
            static stop(elementId) {
                this.withPlayer(p => {
                    this._runningAnimations[elementId].animation.stop();
                });
                return "ok";
            }
            static play(elementId, looped) {
                this.withPlayer(p => {
                    const a = this._runningAnimations[elementId].animation;
                    a.loop = looped;
                    a.play();
                });
                return "ok";
            }
            static kill(elementId) {
                this.withPlayer(p => {
                    this._runningAnimations[elementId].animation.destroy();
                    delete this._runningAnimations[elementId];
                });
                return "ok";
            }
            static pause(elementId) {
                this.withPlayer(p => {
                    this._runningAnimations[elementId].animation.pause();
                });
                return "ok";
            }
            static resume(elementId) {
                this.withPlayer(p => {
                    this._runningAnimations[elementId].animation.play();
                });
                return "ok";
            }
            static needNewPlayerAnimation(current, newProperties) {
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
            static updateProperties(runningAnimation, newProperties) {
                const animation = runningAnimation.animation;
                const runningProperties = runningAnimation.properties;
                if (runningProperties == null || newProperties.rate != runningProperties.rate) {
                    animation.setSpeed(newProperties.rate);
                }
                runningAnimation.properties = newProperties;
            }
            static createAnimation(properties) {
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
            static getPlayerConfig(properties) {
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
                    renderer: "svg",
                    container: document.getElementById(properties.elementId),
                    rendererSettings: {
                        // https://github.com/airbnb/lottie-web/wiki/Renderer-Settings
                        preserveAspectRatio: scaleMode
                    }
                };
                return playerConfig;
            }
            static withPlayer(action) {
                if (Lottie._player) {
                    action(Lottie._player);
                }
                else {
                    require(["lottie"], (p) => {
                        if (!Lottie._player) {
                            Lottie._player = p;
                        }
                        action(p);
                    });
                }
            }
        }
        Lottie._runningAnimations = {};
        UI.Lottie = Lottie;
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
