namespace Windows.Gaming.Input {
	export class Gamepad {

		private static dispatchGamepadAdded: (id: number) => number;
		private static dispatchGamepadRemoved: (id: number) => number;

		public static getConnectedGamepadIds(): string {
			const gamepads = navigator.getGamepads();
			const separator = ";";
			var result = '';
			for (var gamepad of gamepads) {
				if (gamepad) {
					result += gamepad.index + separator;
				}
			}
			return result;
		}

		public static getReading(id: number): string {
			var gamepad = navigator.getGamepads()[id];
			if (!gamepad) {
				return "";
			}

			var result = "";

			result += gamepad.timestamp;

			result += '*';

			for (var axisId = 0; axisId < gamepad.axes.length; axisId++) {
				if (axisId != 0) {
					result += '|';
				}
				result += gamepad.axes[axisId];
			}

			result += '*';

			for (var buttonId = 0; buttonId < gamepad.buttons.length; buttonId++) {
				if (buttonId != 0) {
					result += '|';
				}
				result += gamepad.buttons[buttonId].value;
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

		private static onGamepadConnected(e: any) {
			if (!Gamepad.dispatchGamepadAdded) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					Gamepad.dispatchGamepadAdded = (<any>globalThis).DotnetExports.Uno.Windows.Gaming.Input.Gamepad.DispatchGamepadAdded;
				} else {
					throw `Unable to find dotnet exports`;
				}
			}
			Gamepad.dispatchGamepadAdded(e.gamepad.index);
		}

		private static onGamepadDisconnected(e: any) {
			if (!Gamepad.dispatchGamepadRemoved) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					Gamepad.dispatchGamepadRemoved = (<any>globalThis).DotnetExports.Uno.Windows.Gaming.Input.Gamepad.DispatchGamepadRemoved;
				} else {
					throw `Unable to find dotnet exports`;
				}
			}
			Gamepad.dispatchGamepadRemoved(e.gamepad.index);
		}
	}
}
