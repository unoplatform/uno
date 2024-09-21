namespace Microsoft.UI.Xaml.Input {

	export class FocusVisual {

		private static focusVisualId: number;
		private static focusVisual: HTMLElement | SVGElement;
		private static focusedElement: HTMLElement | SVGElement;
		private static currentDispatchTimeout?: number;

		private static dispatchPositionChange: (id: number) => number;

		public static attachVisual(focusVisualId: number, focusedElementId: number): void {
			FocusVisual.focusVisualId = focusVisualId;
			FocusVisual.focusVisual = Uno.UI.WindowManager.current.getView(focusVisualId);
			FocusVisual.focusedElement = Uno.UI.WindowManager.current.getView(focusedElementId);

			document.addEventListener("scroll", FocusVisual.onDocumentScroll, true);
		}

		public static detachVisual(): void {
			document.removeEventListener("scroll", FocusVisual.onDocumentScroll, true);

			FocusVisual.focusVisualId = null;
		}

		private static onDocumentScroll() {
			if (!FocusVisual.dispatchPositionChange) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					FocusVisual.dispatchPositionChange = (<any>globalThis).DotnetExports.UnoUI.Uno.UI.Xaml.Controls.SystemFocusVisual.DispatchNativePositionChange;
				} else {
					throw `Unable to find dotnet exports`;
				}
			}

			FocusVisual.updatePosition();

			// Throttle managed notification while actively scrolling
			if (FocusVisual.currentDispatchTimeout) {
				clearTimeout(FocusVisual.currentDispatchTimeout);
			}
			FocusVisual.currentDispatchTimeout = setTimeout(() => FocusVisual.dispatchPositionChange(FocusVisual.focusVisualId), 100);
		}

		public static updatePosition(): void {
			const focusVisual = FocusVisual.focusVisual;
			const focusedElement = FocusVisual.focusedElement;

			const boundingRect = focusedElement.getBoundingClientRect();
			const centerX = boundingRect.x + boundingRect.width / 2;
			const centerY = boundingRect.y + boundingRect.height / 2;

			focusVisual.style.setProperty("left", boundingRect.x + "px");
			focusVisual.style.setProperty("top", boundingRect.y + "px");
		}
	}
}
