using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.UI;
using Uno.Disposables;
using Windows.System;

#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	partial class ToolTipService
	{
		#region DependencyProperty: ToolTip

		public static DependencyProperty ToolTipProperty { get; } =
			DependencyProperty.RegisterAttached(
				"ToolTip",
				typeof(object),
				typeof(ToolTipService),
				new FrameworkPropertyMetadata(default, OnToolTipChanged));

		public static object GetToolTip(DependencyObject element) => element.GetValue(ToolTipProperty);
		public static void SetToolTip(DependencyObject element, object value) => element.SetValue(ToolTipProperty, value);

		#endregion
		#region DependencyProperty: Placement

		public static DependencyProperty PlacementProperty { get; } = DependencyProperty.RegisterAttached(
			"Placement",
			typeof(PlacementMode),
			typeof(ToolTipService),
			new FrameworkPropertyMetadata(PlacementMode.Top, OnPlacementChanged));

		public static PlacementMode GetPlacement(DependencyObject element) => (PlacementMode)element.GetValue(PlacementProperty);
		public static void SetPlacement(DependencyObject element, PlacementMode value) => element.SetValue(PlacementProperty, value);

		#endregion
		#region DependencyProperty: ToolTipReference

		internal static DependencyProperty ToolTipReferenceProperty { get; } = DependencyProperty.RegisterAttached(
			"ToolTipReference",
			typeof(ToolTip),
			typeof(ToolTipService),
			new FrameworkPropertyMetadata(default(ToolTip)));

		internal static ToolTip GetToolTipReference(DependencyObject obj) => (ToolTip)obj.GetValue(ToolTipReferenceProperty);
		internal static void SetToolTipReference(DependencyObject obj, ToolTip value) => obj.SetValue(ToolTipReferenceProperty, value);

		#endregion

		private static ToolTip m_CurrentToolTip;
		private static uint m_LastEnteredFrameId;
		private static DispatcherTimer m_OpenTimer;
		private static DispatcherTimer m_CloseTimer;

		private static void OnToolTipChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs e)
		{
			if (!FeatureConfiguration.ToolTip.UseToolTips)
			{
				return; // ToolTips are disabled
			}

			if (!(dependencyobject is FrameworkElement owner))
			{
				return;
			}

			if (e.NewValue is null)
			{
				DisposePreviousToolTip();
			}
			else if (e.NewValue is ToolTip newToolTip)
			{
				var previousToolTip = GetToolTipReference(owner);

				// dispose the previous tooltip
				if (previousToolTip != null && newToolTip != previousToolTip)
				{
					DisposePreviousToolTip(previousToolTip);
				}

				// setup new tooltip
				if (newToolTip != previousToolTip)
				{
					SetupToolTip(newToolTip);
				}
			}
			else
			{
				var previousToolTip = GetToolTipReference(owner);
				if (e.OldValue is ToolTip oldPrevious && oldPrevious == previousToolTip)
				{
					// dispose and setup a new tooltip
					// to avoid corrupting previous tooltip's content with new value
					DisposePreviousToolTip(previousToolTip);
					SetupToolTip(new ToolTip { Content = e.NewValue });
				}
				else if (previousToolTip != null)
				{
					// update the old tooltip with new content
					previousToolTip.Content = e.NewValue;
				}
				else
				{
					// setup a new tooltip
					SetupToolTip(new ToolTip { Content = e.NewValue });
				}
			}

			void SetupToolTip(ToolTip toolTip)
			{
				toolTip.Placement = GetPlacement(toolTip);
				toolTip.SetAnchor(GetPlacementTarget(owner) ?? owner);

				SetToolTipReference(owner, toolTip);
				toolTip.OwnerEventSubscriptions = SubscribeToEvents(owner, toolTip);
			}
			void DisposePreviousToolTip(ToolTip toolTip = null)
			{
				toolTip ??= GetToolTipReference(owner);

				toolTip.OwnerEventSubscriptions?.Dispose();
				toolTip.OwnerEventSubscriptions = null;
				SetToolTipReference(owner, null);
			}
		}

		private static void OnPlacementChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs e)
		{
			if (GetToolTipReference(dependencyobject) is { } tooltip)
			{
				tooltip.Placement = (PlacementMode)e.NewValue;
			}
		}

		private static void OpenToolTipImpl(ToolTip toolTip)
		{
			if (m_CurrentToolTip is { })
			{
				// Only one instance of the tooltip can be opened at any time.
				CloseToolTipImpl(m_CurrentToolTip);
			}

			if (toolTip is { })
			{
				m_CurrentToolTip = toolTip;

				m_OpenTimer.Start();
				m_CloseTimer?.Stop();
			}
		}

		private static void CloseToolTipImpl(ToolTip toolTip)
		{
			if (m_CurrentToolTip == toolTip)
			{
				m_OpenTimer?.Stop();
				m_CloseTimer?.Stop();

				m_CurrentToolTip.IsOpen = false;
			}
			else
			{
				toolTip.IsOpen = false;
			}
		}

		private static void OnOpenTimerTick(object sender, object e)
		{
			m_OpenTimer.Stop();

			if (m_CurrentToolTip is { })
			{
				m_CurrentToolTip.IsOpen = true;
			}

			if (m_CloseTimer is null)
			{
				m_CloseTimer = new DispatcherTimer();
				m_CloseTimer.Interval = TimeSpan.FromMilliseconds(FeatureConfiguration.ToolTip.ShowDuration);
				m_CloseTimer.Tick += OnCloseTimerTick;
			}
			m_CloseTimer.Start();
		}

		private static void OnCloseTimerTick(object sender, object e)
		{
			m_CloseTimer.Stop();

			if (m_CurrentToolTip is { })
			{
				m_CurrentToolTip.IsOpen = false;
			}
		}

		private static IDisposable SubscribeToEvents(FrameworkElement control, ToolTip toolTip)
		{
			// event subscriptions
			if (control.IsLoaded)
			{
				OnOwnerLoaded(control, null);
			}
			control.Loaded += OnOwnerLoaded;
			control.Unloaded += OnOwnerUnloaded;

			return Disposable.Create(() =>
			{
				control.Loaded -= OnOwnerLoaded;
				control.Unloaded -= OnOwnerUnloaded;
				OnOwnerUnloaded(control, null);

				CloseToolTipImpl(toolTip);
			});
		}

		private static void OnOwnerVisibilityChanged(DependencyObject sender, DependencyProperty dp)
		{
			if (sender is FrameworkElement owner && owner.Visibility != Visibility.Visible)
			{
				if (GetToolTipReference(owner) is { } toolTip)
				{
					CloseToolTipImpl(toolTip);
				}
			}
		}

		private static void OnOwnerLoaded(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement owner && GetToolTipReference(owner) is { } toolTip)
			{
				owner.PointerEntered += OnPointerEntered;
				owner.PointerExited += OnPointerExited;
				owner.Tapped += OnTapped;
				owner.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
				if (owner is ButtonBase)
				{
					owner.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnPointerPressed), true);
				}
				var token = owner.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, OnOwnerVisibilityChanged);
				toolTip.OwnerVisibilitySubscription = Disposable.Create(() =>
				{
					owner.UnregisterPropertyChangedCallback(UIElement.VisibilityProperty, token);
				});
			}
		}

		private static void OnOwnerUnloaded(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement owner && GetToolTipReference(owner) is { } toolTip)
			{
				CloseToolTipImpl(toolTip);

				owner.PointerEntered -= OnPointerEntered;
				owner.PointerExited -= OnPointerExited;
				owner.Tapped -= OnTapped;
				owner.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
				if (owner is ButtonBase)
				{
					owner.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnPointerPressed));
				}
				toolTip.OwnerVisibilitySubscription?.Dispose();
				toolTip.OwnerVisibilitySubscription = null;
			}
		}

		private static void OnPointerEntered(object sender, PointerRoutedEventArgs e)
		{
			// Multiple elements can all receive the same PointerEntered at once (from inner-most to outer-most).
			// In this case, the inner-most one is the only one that should be shown,
			// so we are dropping any subsequent events from this frame-id.
			if (e.FrameId == m_LastEnteredFrameId) return;

			if (sender is FrameworkElement owner && GetToolTipReference(owner) is { } toolTip)
			{
				if (toolTip.IsOpen) return;

				if (m_OpenTimer is null)
				{
					m_OpenTimer = new DispatcherTimer();
					m_OpenTimer.Interval = TimeSpan.FromMilliseconds(FeatureConfiguration.ToolTip.ShowDelay);
					m_OpenTimer.Tick += OnOpenTimerTick;
				}
				m_LastEnteredFrameId = e.FrameId;
				OpenToolTipImpl(toolTip);
			}
		}

		private static void OnPointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (sender is FrameworkElement owner && GetToolTipReference(owner) is { } toolTip)
			{
				CloseToolTipImpl(toolTip);
			}
		}

		private static void OnTapped(object sender, TappedRoutedEventArgs e)
		{
			if (sender is FrameworkElement owner && GetToolTipReference(owner) is { } toolTip)
			{
				CloseToolTipImpl(toolTip);
			}
		}

		private static void OnKeyDown(object sender, KeyRoutedEventArgs args)
		{
			if (sender is FrameworkElement owner && GetToolTipReference(owner) is { } toolTip)
			{
				switch (args.Key)
				{
					case VirtualKey.Up:
					case VirtualKey.Down:
					case VirtualKey.Left:
					case VirtualKey.Right:
						return;
				}

				CloseToolTipImpl(toolTip);
			}
		}

		private static void OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender is FrameworkElement owner && GetToolTipReference(owner) is { } toolTip)
			{
				if (e.GetCurrentPoint(owner).Properties.IsLeftButtonPressed)
				{
					CloseToolTipImpl(toolTip);
				}
			}
		}
	}
}
