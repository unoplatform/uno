#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls
{
	/// <summary>
	/// Enumeration of the possible updates modes of a <see cref="Microsoft.UI.Xaml.Controls.ScrollViewer"/>.
	/// </summary>
	public enum ScrollViewerUpdatesMode
	{
		/// <summary>
		/// The ViewChanged event is raised and the VerticalOffset and HorizontalOffset are updated
		/// as soon as the native scroll presenter notifies an update.
		/// </summary>
		/// <remarks>This mode is required to support advance scrolling feature like parallax effect.</remarks>
		Synchronous,

		/// <summary>
		/// The raise of ViewChanged event and the VerticalOffset and HorizontalOffset updates are
		/// queued and dispatcher one idle dispatcher. Some updates may be dropped if the dispatcher is busy,
		/// but the last update will always be notified synchronously.
		/// </summary>
		/// <remarks>This mode is the closest to the windows behavior.</remarks>
		AsynchronousIdle
	}

	/// <summary>
	/// The configurations of the <see cref="Microsoft.UI.Xaml.Controls.ScrollViewer"/> specific to the Uno platform.
	/// </summary>
	public static class ScrollViewer
	{
		/// <summary>
		/// Backing property for the <see cref="ScrollViewerUpdatesMode"/> of a ScrollViewer.
		/// </summary>
		public static DependencyProperty UpdatesModeProperty
		{
			[DynamicDependency(nameof(GetUpdatesMode))]
			[DynamicDependency(nameof(SetUpdatesMode))]
			get;
		} = DependencyProperty.RegisterAttached(
			"UpdatesMode",
			typeof(ScrollViewerUpdatesMode),
			typeof(ScrollViewer),
			new FrameworkPropertyMetadata(
				FeatureConfiguration.ScrollViewer.DefaultUpdatesMode,
				(snd, e) => ((Microsoft.UI.Xaml.Controls.ScrollViewer)snd).UpdatesMode = (ScrollViewerUpdatesMode)e.NewValue));

		/// <summary>
		/// Sets the <see cref="ScrollViewerUpdatesMode"/> of a ScrollViewer.
		/// </summary>
		/// <param name="scrollViewer">The target ScrollViewer to configure</param>
		/// <param name="mode">The updates mode to set</param>
		public static void SetUpdatesMode(Microsoft.UI.Xaml.Controls.ScrollViewer scrollViewer, ScrollViewerUpdatesMode mode)
			=> scrollViewer.SetValue(UpdatesModeProperty, mode);

		/// <summary>
		/// Gets the <see cref="ScrollViewerUpdatesMode"/> of a ScrollViewer.
		/// </summary>
		/// <param name="scrollViewer">The target ScrollViewer</param>
		/// <returns>The updates mode of the <paramref name="scrollViewer"/>.</returns>
		public static ScrollViewerUpdatesMode GetUpdatesMode(Microsoft.UI.Xaml.Controls.ScrollViewer scrollViewer)
			=> (ScrollViewerUpdatesMode)scrollViewer.GetValue(UpdatesModeProperty);

		/// <summary>
		/// Getter for ShouldFallBackToNativeScrollBars attached property. If true, and no <see cref="Microsoft.UI.Xaml.Primitives.ScrollBar"/> is
		/// found in <paramref name="scrollViewer"/>'s template, then native scroll bars (for platforms where available) will be shown instead.
		/// If false, no scroll bars will be shown. True by default, for backward-compatibility.
		/// </summary>
		/// <param name="scrollViewer"></param>
		/// <returns></returns>
		public static bool GetShouldFallBackToNativeScrollBars(Microsoft.UI.Xaml.Controls.ScrollViewer scrollViewer)
		{
			return (bool)scrollViewer.GetValue(ShouldFallBackToNativeScrollBarsProperty);
		}

		/// <summary>
		/// Setter for ShouldFallBackToNativeScrollBars attached property. If true, and no <see cref="Microsoft.UI.Xaml.Primitives.ScrollBar"/> is
		/// found in <paramref name="scrollViewer"/>'s template, then native scroll bars (for platforms where available) will be shown instead.
		/// If false, no scroll bars will be shown. True by default, for backward-compatibility.
		/// </summary>
		public static void SetShouldFallBackToNativeScrollBars(Microsoft.UI.Xaml.Controls.ScrollViewer scrollViewer, bool value)
		{
			scrollViewer.SetValue(ShouldFallBackToNativeScrollBarsProperty, value);
		}

		[DynamicDependency(nameof(GetShouldFallBackToNativeScrollBars))]
		[DynamicDependency(nameof(SetShouldFallBackToNativeScrollBars))]
		public static readonly DependencyProperty ShouldFallBackToNativeScrollBarsProperty =
			DependencyProperty.RegisterAttached("ShouldFallBackToNativeScrollBars", typeof(bool), typeof(ScrollViewer), new FrameworkPropertyMetadata(true));


	}
}
