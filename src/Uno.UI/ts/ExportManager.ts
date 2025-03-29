// eslint-disable-next-line @typescript-eslint/no-namespace
namespace Uno.UI {

	export class ExportManager {

		public static async initialize(): Promise<void> {

			await Windows.ApplicationModel.Core.CoreApplication.initializeExports();

			if ((<any>Module).getAssemblyExports !== undefined) {
				const unoUIExports = await (<any>Module).getAssemblyExports("Uno.UI");

				if (Object.entries(unoUIExports).length > 0) {

					// DotnetExports may already have been initialized
					(<any>globalThis).DotnetExports = (<any>globalThis).DotnetExports || {};

					(<any>globalThis).DotnetExports.UnoUI = unoUIExports;
				}
			}
		}
	}
}
