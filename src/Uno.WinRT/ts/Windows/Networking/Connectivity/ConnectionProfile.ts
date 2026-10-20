namespace Windows.Networking.Connectivity {

	export class ConnectionProfile {
		public static hasInternetAccess(): boolean {
			return navigator.onLine;
		}
	}
}
