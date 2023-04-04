// @ts-nocheck
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        var Media;
        (function (Media) {
            class HtmlMediaPlayer {
                static videoWidth(htmlId) {
                    return document.getElementById(htmlId.toString()).videoWidth;
                }
                static videoHeight(htmlId) {
                    return document.getElementById(htmlId.toString()).videoHeight;
                }
                static getCurrentPosition(htmlId) {
                    return document.getElementById(htmlId.toString()).currentTime;
                }
                static setCurrentPosition(htmlId, currentTime) {
                    document.getElementById(htmlId.toString()).currentTime = currentTime;
                }
                static reload(htmlId) {
                    var element = Uno.UI.WindowManager.current.getView(htmlId.toString());
                    element.load();
                }
                static setVolume(htmlId, volume) {
                    var element = Uno.UI.WindowManager.current.getView(htmlId.toString());
                    element.volume = volume;
                }
                static getDuration(htmlId) {
                    return document.getElementById(htmlId.toString()).duration;
                }
                static setAutoPlay(htmlId, enabled) {
                    var element = Uno.UI.WindowManager.current.getView(htmlId.toString());
                    element.autoplay = enabled;
                }
                static requestFullScreen(htmlId) {
                    var elem = Uno.UI.WindowManager.current.getView(htmlId.toString());
                    var fullscreen = elem.requestFullscreen
                        || elem.webkitRequestFullscreen
                        || elem.mozRequestFullScreen
                        || elem.msRequestFullscreen;
                    fullscreen.call(elem);
                }
                static exitFullScreen() {
                    var closeFullScreen = document.exitFullscreen
                        || document.mozExitFullscreen
                        || document.webkitExitFullscreen
                        || document.msExitFullscreen;
                    closeFullScreen.call(document);
                }
                static pause(htmlId) {
                    var element = Uno.UI.WindowManager.current.getView(htmlId.toString());
                    element.pause();
                }
                static play(htmlId) {
                    var element = Uno.UI.WindowManager.current.getView(htmlId.toString());
                    element.play();
                }
                static stop(htmlId) {
                    var element = Uno.UI.WindowManager.current.getView(htmlId.toString());
                    element.pause();
                    element.currentTime = 0;
                }
            }
            Media.HtmlMediaPlayer = HtmlMediaPlayer;
        })(Media = UI.Media || (UI.Media = {}));
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
