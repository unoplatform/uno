namespace Windows.Devices.Geolocation {

	enum GeolocationAccessStatus {
		Allowed = "Allowed",
		Denied = "Denied",
		Unspecified = "Unspecified"
	}

	export class Geolocator {

		private static dispatchAccessRequest: (serializedAccessStatus: string) => number;

		public static initialize() {
			if (!this.dispatchAccessRequest) {
				this.dispatchAccessRequest = (<any>Module).mono_bind_static_method("[Uno] Windows.Devices.Geolocation.Geolocator:DispatchAccessRequest");
			}
		}

		public static requestAccess() {
			Geolocator.initialize();
			if (navigator.geolocation) {
				navigator.geolocation.getCurrentPosition(
					(_) => { Geolocator.dispatchAccessRequest(GeolocationAccessStatus.Allowed); },
					(error) => {
						if (error.code == error.PERMISSION_DENIED) {
							Geolocator.dispatchAccessRequest(GeolocationAccessStatus.Denied);
						}
						else if (
							error.code == error.POSITION_UNAVAILABLE ||
							error.code == error.TIMEOUT) {
							//position unavailable but we still have permission
							Geolocator.dispatchAccessRequest(GeolocationAccessStatus.Allowed);
						} else {
							Geolocator.dispatchAccessRequest(GeolocationAccessStatus.Unspecified);
						}
					},
					{ enableHighAccuracy: false, maximumAge: 86400000, timeout: 100 });
			} else {
				Geolocator.dispatchAccessRequest(GeolocationAccessStatus.Unspecified);
			}
		}
	}
}
