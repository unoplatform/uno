interface Navigator {
	setAppBadge(value: number):void;
	clearAppBadge(): void;
}

namespace Windows.UI.Notifications {

	export class BadgeUpdater {

		public static setNumber(value: number) {
			if (navigator.setAppBadge) {
				navigator.setAppBadge(value);
			}
		}

		public static clear() {
			if (navigator.clearAppBadge) {
				navigator.clearAppBadge();
			}
		}

	}

}
