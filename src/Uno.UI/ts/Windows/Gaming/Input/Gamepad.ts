namespace Windows.Gaming.Input {
	export class Gamepad {

		private static dispatchGamepadAdded: (id: string) => number;
		private static dispatchGamepadRemoved: (id: string) => number;

		public static getConnectedGamepadIds(): string {
			const gamepads = navigator.getGamepads();
			const separator = ";";
			var result = '';
			for (var i = 0; i < gamepads.length; i++) {
				if (gamepads[i]) {
					result += gamepads[i].index + separator;
				}
			}
			return result;
		}

		public static startGamepadAdded() {
			window.addEventListener("gamepadconnected", Gamepad.onGamepadConnected);
		}

		public static endGamepadAdded() {
			window.removeEventListener("gamepadconnected", Gamepad.onGamepadConnected);
		}

		public static startGamepadRemoved() {
			window.addEventListener("gamepaddisconnected", Gamepad.onGamepadDisconnected);
		}

		public static endGamepadRemoved() {
			window.removeEventListener("gamepaddisconnected", Gamepad.onGamepadDisconnected);
		}

		private static onGamepadConnected(e : any) {
			if (!Gamepad.dispatchGamepadAdded) {
				Gamepad.dispatchGamepadAdded = (<any>Module).mono_bind_static_method(
					"[Uno] Windows.Gaming.Input.Gamepad:DispatchGamepadAdded");
			}
			Gamepad.dispatchGamepadAdded(e.gamepad.index.toString());
		}

		private static onGamepadDisconnected(e : any) {
			if (!Gamepad.dispatchGamepadRemoved) {
				Gamepad.dispatchGamepadRemoved = (<any>Module).mono_bind_static_method(
					"[Uno] Windows.Gaming.Input.Gamepad:DispatchGamepadRemoved");
			}
			Gamepad.dispatchGamepadRemoved(e.gamepad.index.toString());
		}
	}
}
