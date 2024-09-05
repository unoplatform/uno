namespace Uno.UI.Interop {
	export class Runtime {
		public static readonly engine  = Runtime.init();

		private static init(): any {
			return "";
		}

		public static InvokeJS(command: string) : string {
			return eval(command);
		}
	}
}
