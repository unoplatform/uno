using System;
using System.Runtime.InteropServices.JavaScript;

namespace __Windows.UI.Xaml.Media.Animation
{
	internal partial class RenderingLoopAnimator
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.UI.Xaml.Media.Animation.RenderingLoopAnimator.createInstance")]
			internal static partial void CreateInstance(IntPtr managedHandle, double id);

			[JSImport("globalThis.Windows.UI.Xaml.Media.Animation.RenderingLoopAnimator.destroyInstance")]
			internal static partial void DestroyInstance(double jsHandle);

			[JSImport("globalThis.Windows.UI.Xaml.Media.Animation.RenderingLoopAnimator.disableFrameReporting")]
			internal static partial void DisableFrameReporting(double jsHandle);

			[JSImport("globalThis.Windows.UI.Xaml.Media.Animation.RenderingLoopAnimator.enableFrameReporting")]
			internal static partial void EnableFrameReporting(double jsHandle);

			[JSImport("globalThis.Windows.UI.Xaml.Media.Animation.RenderingLoopAnimator.setAnimationFramesInterval")]
			internal static partial void SetAnimationFramesInterval(double jsHandle);

			[JSImport("globalThis.Windows.UI.Xaml.Media.Animation.RenderingLoopAnimator.setStartFrameDelay")]
			internal static partial void SetStartFrameDelay(double jsHandle, double delayMs);
		}
	}
}
