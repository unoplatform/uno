namespace Microsoft.UI.Xaml.Media {
	export class CompositionTarget {
		static requestRender: any;

		static async buildImports() {
			if (CompositionTarget.requestRender === undefined) {
				const exports = await (<any>window.Module).getAssemblyExports("Uno.UI");
				CompositionTarget.requestRender = () => exports.Microsoft.UI.Xaml.Media.CompositionTarget.FrameCallback();
			}
		}

		static requestFrame() {
			CompositionTarget.buildImports().then(() => {
				window.requestAnimationFrame(CompositionTarget.requestRender);
			})
		}
	}
}
