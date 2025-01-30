using System;
using System.Runtime.InteropServices.JavaScript;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Media.Animation
{
	internal partial class RenderingLoopAnimator
	{
		private static WeakEventHelper.WeakEventCollection _frameHandlers = new();

		internal static IDisposable RegisterFrameEvent(Action action)
		{
			var requiresEnable = _frameHandlers.IsEmpty;

			var disposable = WeakEventHelper.RegisterEvent(
				_frameHandlers,
				action,
				(h, s, a) => (h as Action)?.Invoke());

			if (requiresEnable)
			{
				SetEnabledNative(true);
			}

			return Disposable.Create(() =>
			{
				disposable.Dispose();

				if (_frameHandlers.IsEmpty)
				{
					SetEnabledNative(false);
				}
			});
		}

		[JSExport]
		public static void OnFrame()
		{
			_frameHandlers.Invoke(null, null);

			if (_frameHandlers.IsEmpty)
			{
				SetEnabledNative(false);
			}
		}

		private static void SetEnabledNative(bool enabled)
		{
			if (typeof(RenderingLoopAnimator).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(RenderingLoopAnimator).Log().Trace($"SetEnabledNative: {enabled}");
			}

			NativeMethods.SetEnabled(enabled);
		}

		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Microsoft.UI.Xaml.Media.Animation.RenderingLoopAnimator.setEnabled")]
			internal static partial void SetEnabled(bool enabled);
		}
	}
}
