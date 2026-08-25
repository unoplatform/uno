using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
#if HAS_UNO
using Microsoft.UI.Xaml.Media;
#endif

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
			if (sender is not ScrollBar scrollBar)
			{
				return;
			}

			if (args.NewValue is true)
			{
				scrollBar.Loaded += OnScrollBarLoaded;
				if (scrollBar.IsLoaded)
				{
					Enable(scrollBar);
				}
			}
			else
			{
				scrollBar.Loaded -= OnScrollBarLoaded;
				Disable(scrollBar);
			}
#endif
		}

#if HAS_UNO
		private static void OnScrollBarLoaded(object sender, RoutedEventArgs args)
		{
			if (sender is ScrollBar scrollBar)
			{
				Enable(scrollBar);
			}
		}

		private static void Enable(ScrollBar scrollBar)
		{
			SetThumbsIgnoreTouchInput(scrollBar, ignoreTouchInput: false);

			// The interactive template parts are only hit-testable in the MouseIndicator state, and touch
			// has no hover to raise it with, so hold the bar in that state for as long as this is enabled.
			EnsureMouseIndicator(scrollBar);

			if (GetIndicatorModeToken(scrollBar) is null)
			{
				var token = scrollBar.RegisterPropertyChangedCallback(
					ScrollBar.IndicatorModeProperty,
					(snd, _) => EnsureMouseIndicator((ScrollBar)snd));

				SetIndicatorModeToken(scrollBar, token);
			}
		}

		private static void Disable(ScrollBar scrollBar)
		{
			SetThumbsIgnoreTouchInput(scrollBar, ignoreTouchInput: true);

			if (GetIndicatorModeToken(scrollBar) is { } token)
			{
				scrollBar.UnregisterPropertyChangedCallback(ScrollBar.IndicatorModeProperty, token);
				SetIndicatorModeToken(scrollBar, null);
			}
		}

		private static void EnsureMouseIndicator(ScrollBar scrollBar)
		{
			if (scrollBar.IndicatorMode != ScrollingIndicatorMode.MouseIndicator)
			{
				scrollBar.IndicatorMode = ScrollingIndicatorMode.MouseIndicator;
			}
		}

		private static void SetThumbsIgnoreTouchInput(DependencyObject root, bool ignoreTouchInput)
		{
			var count = VisualTreeHelper.GetChildrenCount(root);
			for (var i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);
				if (child is Thumb thumb)
				{
					thumb.IgnoreTouchInput = ignoreTouchInput;
				}

				SetThumbsIgnoreTouchInput(child, ignoreTouchInput);
			}
		}

		private static DependencyProperty IndicatorModeTokenProperty { get; } =
			DependencyProperty.RegisterAttached(
				"IndicatorModeToken",
				typeof(long?),
				typeof(ScrollBarExtensions),
				new PropertyMetadata(null)
			);

		private static long? GetIndicatorModeToken(ScrollBar scrollBar)
			=> (long?)scrollBar.GetValue(IndicatorModeTokenProperty);

		private static void SetIndicatorModeToken(ScrollBar scrollBar, long? token)
			=> scrollBar.SetValue(IndicatorModeTokenProperty, token);
#endif

		#endregion
	}
}
