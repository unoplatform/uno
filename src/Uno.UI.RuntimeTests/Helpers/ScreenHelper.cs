using System;
using System.Collections.Generic;
using System.Text;
using Uno.Disposables;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;

namespace Uno.UI.RuntimeTests.Helpers
{
	public static class ScreenHelper
	{
		/// <summary>
		/// Temporarily override the <see cref="ApplicationView.VisibleBounds"/> of the app. This can be used to test VisibleBounds-related
		/// behaviour on targets that ordinarily don't have an 'unsafe area'.
		/// </summary>
		/// <param name="unsafeArea">The 'unsafe area' to apply, ie the margin between the total window bounds and the visible bounds.</param>
		/// <param name="skipIfHasNativeUnsafeArea">
		/// If true, and the current target already has a non-zero safe area, then the native visible bounds will be used. If false, the
		/// override will always be used, in place of the native visible bounds.
		/// </param>
		/// <returns></returns>
		public static IDisposable OverrideVisibleBounds(Thickness unsafeArea, bool skipIfHasNativeUnsafeArea = true)
		{
#if !HAS_UNO
			// Not supported on Windows
			return null;
#else
			var fullBounds = Window.Current.Bounds;
			var applicationView = ApplicationView.GetForCurrentView();
			if (skipIfHasNativeUnsafeArea && fullBounds != applicationView.VisibleBounds)
			{
				return null;
			}
			var height = fullBounds.Height - unsafeArea.Top - unsafeArea.Bottom;
			var width = fullBounds.Width - unsafeArea.Left - unsafeArea.Right;
			var overriddenVisibleBounds = new Rect(unsafeArea.Left, unsafeArea.Top, width, height);
			applicationView.VisibleBoundsOverride = overriddenVisibleBounds;
			return Disposable.Create(() => applicationView.VisibleBoundsOverride = null);
#endif
		}

		public static Thickness GetUnsafeArea()
		{
			var windowBounds = Window.Current.Bounds;
			var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;
			return new Thickness(
				visibleBounds.Left - windowBounds.Left,
				visibleBounds.Top - windowBounds.Top,
				windowBounds.Right - visibleBounds.Right,
				windowBounds.Bottom - visibleBounds.Bottom
			);
		}
	}
}
