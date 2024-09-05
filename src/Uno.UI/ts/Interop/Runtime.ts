namespace Uno.UI.Interop {
	export class Runtime {
		public static readonly engine  = Runtime.init();

		private static init(): any {
			return "";
		}

		public static InvokeJS(command: string): string {
			// Preseve the original emscripten marshalling semantics
			// to always return a valid string.
			return String(eval(command) || "");
		}
	}
}
