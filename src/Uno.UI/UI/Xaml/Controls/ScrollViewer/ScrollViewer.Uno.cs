using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls
{
	/// <summary>
	/// Enumeration of the possible updates modes of a <see cref="Windows.UI.Xaml.Controls.ScrollViewer"/>.
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
	/// The configurations of the <see cref="Windows.UI.Xaml.Controls.ScrollViewer"/> specific to the Uno platform.
	/// </summary>
	public static class ScrollViewer
	{
		/// <summary>
		/// Backing property for the <see cref="ScrollViewerUpdatesMode"/> of a ScrollViewer.
		/// </summary>
		public static readonly DependencyProperty UpdatesModeProperty = DependencyProperty.RegisterAttached(
			"UpdatesMode",
			typeof(ScrollViewerUpdatesMode),
			typeof(ScrollViewer),
			new PropertyMetadata(
				FeatureConfiguration.ScrollViewer.DefaultUpdatesMode,
				(snd, e) => ((Windows.UI.Xaml.Controls.ScrollViewer)snd).UpdatesMode = (ScrollViewerUpdatesMode)e.NewValue));

		/// <summary>
		/// Sets the <see cref="ScrollViewerUpdatesMode"/> of a ScrollViewer.
		/// </summary>
		/// <param name="scrollViewer">The target ScrollViewer to configure</param>
		/// <param name="mode">The updates mode to set</param>
		public static void SetUpdatesMode(Windows.UI.Xaml.Controls.ScrollViewer scrollViewer, ScrollViewerUpdatesMode mode)
			=> scrollViewer.SetValue(UpdatesModeProperty, mode);

		/// <summary>
		/// Gets the <see cref="ScrollViewerUpdatesMode"/> of a ScrollViewer.
		/// </summary>
		/// <param name="scrollViewer">The target ScrollViewer</param>
		/// <returns>The updates mode of the <paramref name="scrollViewer"/>.</returns>
		public static ScrollViewerUpdatesMode GetUpdatesMode(Windows.UI.Xaml.Controls.ScrollViewer scrollViewer)
			=> (ScrollViewerUpdatesMode)scrollViewer.GetValue(UpdatesModeProperty);
	}
}
