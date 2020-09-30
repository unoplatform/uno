interface NavigatorDataTransferManager {
	share(data: any): Promise<void>;
}

interface Navigator extends NavigatorDataTransferManager { }

namespace Windows.ApplicationModel.DataTransfer {

	export class DataTransferManager {

		public static isSupported(): boolean {
			var navigatorAny = navigator as any;
			return typeof navigatorAny.share === "function";
		}

		public static async showShareUI(title: string, text: string, url: string): Promise<string> {
			var data: any = {};
			if (title) {
				data.title = title;
			}
			if (text) {
				data.text = text;
			}
			if (url) {
				data.url = url;
			}

			if (navigator.share) {
				try {
					await navigator.share(data);
					return "true";
				}
				catch (e) {
					console.log("Sharing failed:" + e);
					return "false";
				}
			}
			console.log("navigator.share API is not available in this browser");
			return "false";
		}
	}
}
