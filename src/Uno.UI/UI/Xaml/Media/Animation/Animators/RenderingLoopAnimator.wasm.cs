using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Uno;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Interop;

namespace Windows.UI.Xaml.Media.Animation
{
	internal sealed class RenderingLoopFloatAnimator : CPUBoundFloatAnimator, IJSObject
	{
		public RenderingLoopFloatAnimator(float from, float to)
			: base(from, to)
		{
			Handle = JSObjectHandle.Create(this, Metadata.Instance);
		}

		public JSObjectHandle Handle { get; }


		protected override void EnableFrameReporting() => WebAssemblyRuntime.InvokeJSWithInterop($"{this}.EnableFrameReporting();");

		protected override void DisableFrameReporting() => WebAssemblyRuntime.InvokeJSWithInterop($"{this}.DisableFrameReporting();");

		protected override void SetStartFrameDelay(long delayMs) => WebAssemblyRuntime.InvokeJSWithInterop($"{this}.SetStartFrameDelay({delayMs});");

		protected override void SetAnimationFramesInterval() => WebAssemblyRuntime.InvokeJSWithInterop($"{this}.SetAnimationFramesInterval();");

		private void OnFrame() => OnFrame(null, null);

		private class Metadata : IJSObjectMetadata
		{
			public static Metadata Instance {get;} = new Metadata();
			private Metadata() { }

			private static long _handles = 0L;
			private bool _isPrototypeExported;

			/// <inheritdoc />
			public long CreateNativeInstance(IntPtr managedHandle)
			{
				if (!_isPrototypeExported)
				{
					// Makes type visible to javascript
					WebAssemblyRuntime.InvokeJS(_prototype);
					_isPrototypeExported = true;
				}

				var id = Interlocked.Increment(ref _handles);
				WebAssemblyRuntime.InvokeJS($"Windows.UI.Xaml.Media.Animation.RenderingLoopFloatAnimator.createInstance(\"{managedHandle}\", \"{id}\")");

				return id;
			}

			/// <inheritdoc />
			public string GetNativeInstance(IntPtr managedHandle, long jsHandle)
				=> $"Windows.UI.Xaml.Media.Animation.RenderingLoopFloatAnimator.getInstance(\"{managedHandle}\", \"{jsHandle}\")";

			/// <inheritdoc />
			public void DestroyNativeInstance(IntPtr managedHandle, long jsHandle)
				=> WebAssemblyRuntime.InvokeJS($"Windows.UI.Xaml.Media.Animation.RenderingLoopFloatAnimator.destroyInstance(\"{managedHandle}\", \"{jsHandle}\")");

			/// <inheritdoc />
			public object InvokeManaged(object instance, string method, string parameters)
			{
				switch (method)
				{
					case "OnFrame":
						((RenderingLoopFloatAnimator)instance).OnFrame();
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(method));
				}

				return null;
			}

			// Note: This should be written in TypeScript and embedded in the package.
			private const string _prototype = @"(function() {
	var Windows = window.Windows;
	(function (Windows) {
		var UI = window.UI;
		(function (UI) {
			var Xaml = window.Xaml;
			(function (Xaml) {
				var Media = window.Media;
				(function (Media) {
					var Animation = window.Animation;
					(function (Animation) {
						var RenderingLoopFloatAnimator = (function() {

							RenderingLoopFloatAnimator.activeInstances = {};

							RenderingLoopFloatAnimator.createInstance = function(managedId, jsId) {
								this.activeInstances[jsId] = new RenderingLoopFloatAnimator(managedId);

								return ""ok"";
							}

							RenderingLoopFloatAnimator.getInstance = function(managedId, jsId) {
								return this.activeInstances[jsId];
							}

							RenderingLoopFloatAnimator.destroyInstance = function(managedId, jsId) {
								delete this.activeInstances[jsId];

								return ""ok"";
							}

							function RenderingLoopFloatAnimator(managedHandle) {
								this.__managedHandle = managedHandle;
							};

							RenderingLoopFloatAnimator.prototype.SetStartFrameDelay = function(delay) {
								this.unscheduleFrame();

								if (this._isEnabled) {
									this.scheduleDelayedFrame(delay);
								}
							};

							RenderingLoopFloatAnimator.prototype.SetAnimationFramesInterval = function() {
								this.unscheduleFrame();
								
								if (this._isEnabled) {
									this.onFrame();
								}
							};

							RenderingLoopFloatAnimator.prototype.EnableFrameReporting = function() {
								if (this._isEnabled) {
									return;
								}

								this._isEnabled = true;
								this.scheduleAnimationFrame();
							};

							RenderingLoopFloatAnimator.prototype.DisableFrameReporting = function() {
								this._isEnabled = false;
								this.unscheduleFrame();
							};

							RenderingLoopFloatAnimator.prototype.onFrame = function(timestamp) {
								Uno.Foundation.Interop.ManagedObject.dispatch(this.__managedHandle, ""OnFrame"");

								// Schedule a new frame only if still enabled and no frame was scheduled by the managed OnFrame
								if (this._isEnabled && this._frameRequestId == null && this._delayRequestId == null) {
									this.scheduleAnimationFrame();
								}
							};

							RenderingLoopFloatAnimator.prototype.unscheduleFrame = function(timestamp) {
								if (this._delayRequestId != null) {
									clearTimeout(this._delayRequestId);
									this._delayRequestId = null;
								}
								if (this._frameRequestId != null) {
									window.cancelAnimationFrame(this._frameRequestId);
									this._frameRequestId = null;
								}
							};

							RenderingLoopFloatAnimator.prototype.scheduleDelayedFrame = function(delay) {
									var that = this;
									this._delayRequestId = setTimeout(function() {
										that._delayRequestId = null;
										that.onFrame();
									},
									delay);
							};


							RenderingLoopFloatAnimator.prototype.scheduleAnimationFrame = function() {
									var that = this;
									this._frameRequestId = window.requestAnimationFrame(function(ts) {
										that._frameRequestId = null;
										that.onFrame(ts);
									});
							};

							return RenderingLoopFloatAnimator;

						}());

						Animation.RenderingLoopFloatAnimator = RenderingLoopFloatAnimator;
					})(Animation = Media.Animation || (Media.Animation = {}));
				})(Media = Xaml.Media || (Xaml.Media = {}));
			})(Xaml = UI.Xaml || (UI.Xaml = {}));
		})(UI = Windows.UI || (Windows.UI = {}));
	})(Windows || (Windows = {}));
window.Windows = Windows;

return ""ok"";})();";
		}
	}
}
