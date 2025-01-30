namespace Windows.Devices.Geolocation {

    enum GeolocationAccessStatus {
        Allowed = "Allowed",
        Denied = "Denied",
        Unspecified = "Unspecified"
    }

    enum PositionStatus {
        Ready = "Ready",
        Initializing = "Initializing",
        NoData = "NoData",
        Disabled = "Disabled",
        NotInitialized = "NotInitialized",
        NotAvailable = "NotAvailable"
    }

    export class Geolocator {

        private static dispatchAccessRequest: (serializedAccessStatus: string) => number;
        private static dispatchGeoposition: (geopositionRequestResult: string, requestId: string) => number;
        private static dispatchError: (geopositionRequestResult: string, requestId: string) => number;

		private static interopInitialized: boolean = false;
		
        private static positionWatches: any;

        public static initialize() {
			this.positionWatches = {};

			if (!Geolocator.interopInitialized) {
				const exports: any = (<any>globalThis).DotnetExports?.Uno?.Uno?.Devices?.Geolocation?.Geolocator;

				if (exports !== undefined) {
					Geolocator.dispatchAccessRequest = exports.DispatchAccessRequest;
					Geolocator.dispatchError = exports.DispatchError;
					Geolocator.dispatchGeoposition = exports.DispatchGeoposition;
				}
				else {
					throw `Unable to find dotnet exports`;
				}

				Geolocator.interopInitialized = true;
			}
        }

        //checks for permission to the geolocation services
        public static requestAccess() {
            Geolocator.initialize();
            if (navigator.geolocation) {
                navigator.geolocation.getCurrentPosition(
                    (_) => {
                        Geolocator.dispatchAccessRequest(GeolocationAccessStatus.Allowed);
                    },
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
                Geolocator.dispatchAccessRequest(GeolocationAccessStatus.Denied);
            }
        }

        //retrieves a single geoposition
        public static getGeoposition(
            desiredAccuracyInMeters: number,
            maximumAge: number,
            timeout: number,
            requestId: string) {
            Geolocator.initialize();
            if (navigator.geolocation) {
                this.getAccurateCurrentPosition(
                    (position) => Geolocator.handleGeoposition(position, requestId),
                    (error) => Geolocator.handleError(error, requestId),
                    desiredAccuracyInMeters,
                    {
                        enableHighAccuracy: desiredAccuracyInMeters < 50, //if desired accuracy is over less than 50 meters, more accurate GPS source will be needed
                        maximumAge: maximumAge,
                        timeout: timeout
                    });
            }
            else {
                Geolocator.dispatchError(PositionStatus.NotAvailable, requestId);
            }
        }

        public static startPositionWatch(desiredAccuracyInMeters: number, requestId: string): boolean {
            Geolocator.initialize();
            if (navigator.geolocation) {
                Geolocator.positionWatches[requestId] = navigator.geolocation.watchPosition(
                    (position) => Geolocator.handleGeoposition(position, requestId),
                    (error) => Geolocator.handleError(error, requestId));
                return true;
            } else {
                return false;
            }
        }

        public static stopPositionWatch(desiredAccuracyInMeters: number, requestId: string) {
            navigator.geolocation.clearWatch(Geolocator.positionWatches[requestId]);
            delete Geolocator.positionWatches[requestId];
        }

		private static handleGeoposition(position: GeolocationPosition, requestId: string) {
            var serializedGeoposition = position.coords.latitude + ":" +
                position.coords.longitude + ":" +
                position.coords.altitude + ":" +
                position.coords.altitudeAccuracy + ":" +
                position.coords.accuracy + ":" +
                position.coords.heading + ":" +
                position.coords.speed + ":" +
                position.timestamp;
            Geolocator.dispatchGeoposition(serializedGeoposition, requestId);
        }

		private static handleError(error: GeolocationPositionError, requestId: string) {
            if (error.code == error.TIMEOUT) {
                Geolocator.dispatchError(PositionStatus.NoData, requestId);
            } else if (error.code == error.PERMISSION_DENIED) {
                Geolocator.dispatchError(PositionStatus.Disabled, requestId);
            } else if (error.code == error.POSITION_UNAVAILABLE) {
                Geolocator.dispatchError(PositionStatus.NotAvailable, requestId);
            }
        }

        //this attempts to squeeze out the requested accuracy from the GPS by utilizing the set timeout
        //adapted from https://github.com/gregsramblings/getAccurateCurrentPosition/blob/master/geo.js		
        private static getAccurateCurrentPosition(
            geolocationSuccess: PositionCallback,
            geolocationError: PositionErrorCallback,
            desiredAccuracy: Number,
            options: PositionOptions) {
            var lastCheckedPosition: GeolocationPosition;
            var locationEventCount = 0;
            var watchId: number;
            var timerId: number;

			var checkLocation = function (position: GeolocationPosition) {
                lastCheckedPosition = position;
                locationEventCount = locationEventCount + 1;

                //is the accuracy enough?
                if (position.coords.accuracy <= desiredAccuracy) {
                    clearTimeout(timerId);
                    navigator.geolocation.clearWatch(watchId);
                    foundPosition(position);
                }
            };

            var stopTrying = function () {
                navigator.geolocation.clearWatch(watchId);
                foundPosition(lastCheckedPosition);
            };

			var onError = function (error: GeolocationPositionError) {
                clearTimeout(timerId);
                navigator.geolocation.clearWatch(watchId);
                geolocationError(error);
            };

			var foundPosition = function (position: GeolocationPosition) {
                geolocationSuccess(position);
            };

            watchId = navigator.geolocation.watchPosition(checkLocation, onError, options);
            timerId = setTimeout(stopTrying, options.timeout);
        };
    }
}
