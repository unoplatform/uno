interface Window {
    opr: any;
    opera: any;
    mozVibrate(pattern: number | number[]): boolean;
    msVibrate(pattern: number | number[]): boolean;
    InstallTrigger: any;
    HTMLElement: any;
    StyleMedia: any;
    chrome: any;
    CSS: any;
    safari: any;
}

interface Document {
    documentMode: any;
}

namespace Windows.System.Profile {

    export class AnalyticsVersionInfo {
        public static getUserAgent(): string {
            return navigator.userAgent;
        }

        public static getBrowserName(): string {
            // Opera 8.0+
            if ((!!window.opr && !!window.opr.addons) || !!window.opera || navigator.userAgent.indexOf(' OPR/') >= 0) {
                return "Opera";
            }

            // Firefox 1.0+
            if (typeof window.InstallTrigger !== 'undefined') {
                return "Firefox";
            }

            // Safari 3.0+ "[object HTMLElementConstructor]" 
            if (/constructor/i.test(window.HTMLElement) ||
	            ((p: any) => p.toString() === "[object SafariRemoteNotification]")(
		            typeof window.safari !== 'undefined' && window.safari.pushNotification)) {
	            return "Safari";
            }

            // Edge 20+
            if (!!window.StyleMedia) {
	            return "Edge";
            }

            // Chrome 1 - 71
            if (!!window.chrome && (!!window.chrome.webstore || !!window.chrome.runtime)) {
	            return "Chrome";
            }
        }
    }
}
