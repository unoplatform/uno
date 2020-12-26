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

			try {
				await navigator.share(data);
				return "true";
			}
			catch {
				return "false";
			}
		}
	}
}
