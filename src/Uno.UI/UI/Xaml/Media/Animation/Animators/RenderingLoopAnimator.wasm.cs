using System;
using System.Threading;
using Uno.Foundation;
using Uno.Foundation.Interop;

namespace Windows.UI.Xaml.Media.Animation
{
	internal abstract class RenderingLoopAnimator<T> : CPUBoundAnimator<T>, IJSObject where T : struct
	{
		protected RenderingLoopAnimator(T from, T to)
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

			/// <inheritdoc />
			public long CreateNativeInstance(IntPtr managedHandle)
			{
				var id = RenderingLoopAnimatorMetadataIdProvider.Next();
				WebAssemblyRuntime.InvokeJS($"Windows.UI.Xaml.Media.Animation.RenderingLoopFloatAnimator.createInstance(\"{managedHandle}\", \"{id}\")");

				return id;
			}

			/// <inheritdoc />
			public string GetNativeInstance(IntPtr managedHandle, long jsHandle)
				=> $"Windows.UI.Xaml.Media.Animation.RenderingLoopFloatAnimator.getInstance(\"{jsHandle}\")";

			/// <inheritdoc />
			public void DestroyNativeInstance(IntPtr managedHandle, long jsHandle)
				=> WebAssemblyRuntime.InvokeJS($"Windows.UI.Xaml.Media.Animation.RenderingLoopFloatAnimator.destroyInstance(\"{jsHandle}\")");

			/// <inheritdoc />
			public object InvokeManaged(object instance, string method, string parameters)
			{
				switch (method)
				{
					case "OnFrame":
						((RenderingLoopAnimator<T>)instance).OnFrame();
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(method));
				}

				return null;
			}
		}
	}
}
