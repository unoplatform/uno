// eslint-disable-next-line @typescript-eslint/no-namespace
namespace Uno.UI {

	export class ExportManager {

		public static async initialize(): Promise<void> {

			if ((<any>Module).getAssemblyExports !== undefined) {
				const unoExports = await (<any>Module).getAssemblyExports("Uno");
				const unoUIExports = await (<any>Module).getAssemblyExports("Uno.UI");

				(<any>globalThis).DotnetExports = { Uno: unoExports, UnoUI: unoUIExports };
			}
		}
	}
}
