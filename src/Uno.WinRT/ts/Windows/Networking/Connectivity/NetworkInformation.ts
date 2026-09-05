namespace Windows.Networking.Connectivity {

	export class NetworkInformation {
		private static dispatchStatusChanged: () => number;

		public static startStatusChanged() {
			window.addEventListener("online", NetworkInformation.networkStatusChanged);
			window.addEventListener("offline", NetworkInformation.networkStatusChanged);
		}

		public static stopStatusChanged() {
			window.removeEventListener("online", NetworkInformation.networkStatusChanged);
			window.removeEventListener("offline", NetworkInformation.networkStatusChanged);
		}

		public static networkStatusChanged() {
			if (NetworkInformation.dispatchStatusChanged == null && (<any>globalThis).DotnetExports !== undefined) {
				if ((<any>globalThis).Uno.UI.Runtime.Skia.WebAssemblyThreading.isThreadingEnabled()) {
					NetworkInformation.dispatchStatusChanged = (<any>globalThis).DotnetExports.Uno.Windows.Networking.Connectivity.NetworkInformation.DispatchStatusChangedAsync;
				} else {
					NetworkInformation.dispatchStatusChanged = (<any>globalThis).DotnetExports.Uno.Windows.Networking.Connectivity.NetworkInformation.DispatchStatusChanged;
				}
			} else {
				throw `NetworkInformation: Unable to find dotnet exports`;
			}
			NetworkInformation.dispatchStatusChanged();
		}
	}
}
