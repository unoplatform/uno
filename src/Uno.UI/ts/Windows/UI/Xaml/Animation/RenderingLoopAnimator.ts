namespace Microsoft.UI.Xaml.Media.Animation {
	export class RenderingLoopAnimator {
		private static activeInstances: { [jsHandle: number]: RenderingLoopAnimator} = {};

		public static createInstance(managedHandle: string, jsHandle: number) {
			RenderingLoopAnimator.activeInstances[jsHandle] = new RenderingLoopAnimator(managedHandle);
		}

		public static getInstance(jsHandle: number): RenderingLoopAnimator {
			return RenderingLoopAnimator.activeInstances[jsHandle];
		}

		public static destroyInstance(jsHandle: number) {
			var instance = RenderingLoopAnimator.getInstance(jsHandle);
			// If the JSObjectHandle is being disposed before the animator is stopped (GC collecting JSObjectHandle before the animator)
			// we won't be able to DisableFrameReporting anymore.
			if (instance) {
				instance.DisableFrameReporting();
			}
			delete RenderingLoopAnimator.activeInstances[jsHandle];
		}

		private constructor(private managedHandle: string) {
		}

		public SetStartFrameDelay(delay: number) {
			this.unscheduleFrame();

			if (this._isEnabled) {
				this.scheduleDelayedFrame(delay);
			}
		}

		public SetAnimationFramesInterval() {
			this.unscheduleFrame();

			if (this._isEnabled) {
				this.onFrame();
			}
		}

		public EnableFrameReporting() {
			if (this._isEnabled) {
				return;
			}

			this._isEnabled = true;
			this.scheduleAnimationFrame();
		}

		public DisableFrameReporting() {
			this._isEnabled = false;
			this.unscheduleFrame();
		}

		private onFrame() {
			Uno.Foundation.Interop.ManagedObject.dispatch(this.managedHandle, "OnFrame", null);

			// Schedule a new frame only if still enabled and no frame was scheduled by the managed OnFrame
			if (this._isEnabled && this._frameRequestId == null && this._delayRequestId == null) {
				this.scheduleAnimationFrame();
			}
		}

		private unscheduleFrame() {
			if (this._delayRequestId != null) {
				clearTimeout(this._delayRequestId);
				this._delayRequestId = null;
			}
			if (this._frameRequestId != null) {
				window.cancelAnimationFrame(this._frameRequestId);
				this._frameRequestId = null;
			}
		}

		private scheduleDelayedFrame(delay: number) {
			this._delayRequestId = setTimeout(() => {
					this._delayRequestId = null;
					this.onFrame();
				},
				delay);
		}

		private scheduleAnimationFrame() {
			this._frameRequestId = window.requestAnimationFrame(() => {
				this._frameRequestId = null;
				this.onFrame();
			});
		}

		private _delayRequestId?: number;
		private _frameRequestId?: number;
		private _isEnabled = false;
	}
}
