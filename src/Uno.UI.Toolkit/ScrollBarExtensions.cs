using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Uno.UI.Toolkit
{
	public static class ScrollBarExtensions
	{
		#region IsTouchThumbDragEnabled

		/// <summary>
		/// Lets the <see cref="ScrollBar"/> thumb be dragged with a finger.
		/// </summary>
		/// <remarks>
		/// WinUI has the ScrollBar parts ignore touch, as touch scrolling goes through DirectManipulation
		/// there and the bar is only an indicator. A touch-only device - a browser on a tablet most notably -
		/// has no pointer to reveal an interactive bar with, so this opts into keeping the bar in its
		/// interactive state, which also keeps it visible. No effect on Windows.
		/// </remarks>
		public static DependencyProperty IsTouchThumbDragEnabledProperty { get; } =
			DependencyProperty.RegisterAttached(
				"IsTouchThumbDragEnabled",
				typeof(bool),
				typeof(ScrollBarExtensions),
				new PropertyMetadata(false, OnIsTouchThumbDragEnabledChanged)
			);

		public static void SetIsTouchThumbDragEnabled(this ScrollBar scrollBar, bool isTouchThumbDragEnabled)
			=> scrollBar.SetValue(IsTouchThumbDragEnabledProperty, isTouchThumbDragEnabled);

		public static bool GetIsTouchThumbDragEnabled(this ScrollBar scrollBar)
			=> (bool)scrollBar.GetValue(IsTouchThumbDragEnabledProperty);

		private static void OnIsTouchThumbDragEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
#if HAS_UNO
			// The ScrollBar honours this while attaching its template parts, so it also holds for a bar which
			// is deferred (x:Load) by the ScrollViewer template and only realized once an axis overflows.
			if (sender is ScrollBar scrollBar)
			{
				scrollBar.IsTouchThumbDragEnabled = args.NewValue is true;
			}
#endif
		}

		#endregion
	}
}
