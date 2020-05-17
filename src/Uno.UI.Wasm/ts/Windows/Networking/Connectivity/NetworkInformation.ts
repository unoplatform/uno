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
			if (NetworkInformation.dispatchStatusChanged == null) {
				NetworkInformation.dispatchStatusChanged = 
					(<any>Module).mono_bind_static_method(
						"[Uno] Windows.Networking.Connectivity.NetworkInformation:DispatchStatusChanged");				
			}
			NetworkInformation.dispatchStatusChanged();
		}
	}
}
