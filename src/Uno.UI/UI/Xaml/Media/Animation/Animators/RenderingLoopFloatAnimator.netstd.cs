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
								if (this._frameRequestId) {
									window.cancelAnimationFrame(this._frameRequestId);
								}

								if (this._isEnabled) {
									var that = this;
									this._delayRequestId = setTimeout(function() { that.onFrame(); }, delay);
								}
							};

							RenderingLoopFloatAnimator.prototype.SetAnimationFramesInterval = function() {
								if (this._delayRequestId) {
									clearTimeout(this._delayRequestId);
								}
								
								if (this._isEnabled) {
									this.onFrame();
								}
							};

							RenderingLoopFloatAnimator.prototype.EnableFrameReporting = function() {
								if (this._isEnabled) {
									return;
								}

								this._isEnabled = true;
								var that = this;
								this._frameRequestId = window.requestAnimationFrame(function(timestamp) { that.onFrame(timestamp); });
							};

							RenderingLoopFloatAnimator.prototype.DisableFrameReporting = function() {
								this._isEnabled = false;
								if (this._delayRequestId) {
									clearTimeout(this._delayRequestId);
								}
								if (this._frameRequestId) {
									window.cancelAnimationFrame(this._frameRequestId);
								}
							};

							RenderingLoopFloatAnimator.prototype.onFrame = function(timestamp) {
								Uno.Foundation.Interop.ManagedObject.dispatch(this.__managedHandle, ""OnFrame"");
								if (this._isEnabled) {
									var that = this;
									this._frameRequestId = window.requestAnimationFrame(function(timestamp) { that.onFrame(timestamp); });
								}
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
