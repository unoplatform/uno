namespace Microsoft.UI.Xaml.Media.Animation {
	export class RenderingLoopAnimator {

		private static dispatchFrame: () => number;

		private static init() {
			if (!RenderingLoopAnimator.dispatchFrame) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					RenderingLoopAnimator.dispatchFrame = (<any>globalThis).DotnetExports.UnoUI.Microsoft.UI.Xaml.Media.Animation.RenderingLoopAnimator.OnFrame;
				} else {
					throw `Unable to find dotnet exports`;
				}
			}
		}

		public static setEnabled(enabled: boolean) {
			RenderingLoopAnimator.init();

			RenderingLoopAnimator._isEnabled = enabled;

			if (enabled) {
				RenderingLoopAnimator.scheduleAnimationFrame();
			} else if (RenderingLoopAnimator._frameRequestId != null) {
				window.cancelAnimationFrame(RenderingLoopAnimator._frameRequestId);
				RenderingLoopAnimator._frameRequestId = null;
			}
		}

		private static scheduleAnimationFrame() {
			if (RenderingLoopAnimator._frameRequestId == null) {
				RenderingLoopAnimator._frameRequestId = window.requestAnimationFrame(RenderingLoopAnimator.onAnimationFrame);
			}
		}

		private static onAnimationFrame() {
			RenderingLoopAnimator.dispatchFrame();

			RenderingLoopAnimator._frameRequestId = null;

			if (RenderingLoopAnimator._isEnabled) {
				RenderingLoopAnimator.scheduleAnimationFrame();
			}
		}

		private static _frameRequestId?: number;
		private static _isEnabled = false;
	}
}
