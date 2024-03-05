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

		private static IDisposable SubscribeToEvents(FrameworkElement control, ToolTip tooltip)
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

				tooltip.IsOpen = false;
				tooltip.CurrentHoverId++;
			});
		}

		private static void OnOwnerVisibilityChanged(DependencyObject sender, DependencyProperty dp)
		{
			if (sender is FrameworkElement owner && owner.Visibility != Visibility.Visible)
			{
				if (GetToolTipReference(owner) is { } toolTip)
				{
					toolTip.IsOpen = false;
					toolTip.CurrentHoverId++;
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
				toolTip.IsOpen = false;
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
			if (sender is FrameworkElement owner && GetToolTipReference(owner) is { } toolTip)
			{
				_ = HoverTask(++toolTip.CurrentHoverId);
				async Task HoverTask(long hoverId)
				{
					await Task.Delay(FeatureConfiguration.ToolTip.ShowDelay);
					if (toolTip.CurrentHoverId != hoverId)
					{
						return;
					}

					if (owner.IsLoaded)
					{
						toolTip.IsOpen = true;

						await Task.Delay(FeatureConfiguration.ToolTip.ShowDuration);
						if (toolTip.CurrentHoverId == hoverId)
						{
							toolTip.IsOpen = false;
						}
					}
				}
			}
		}

		private static void OnPointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (sender is FrameworkElement owner && GetToolTipReference(owner) is { } toolTip)
			{
				toolTip.IsOpen = false;
				toolTip.CurrentHoverId++;
			}
		}

		private static void OnTapped(object sender, TappedRoutedEventArgs e)
		{
			if (sender is FrameworkElement owner && GetToolTipReference(owner) is { } toolTip)
			{
				toolTip.IsOpen = false;
				toolTip.CurrentHoverId++;
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
				toolTip.IsOpen = false;
				toolTip.CurrentHoverId++;
			}
		}

		private static void OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender is FrameworkElement owner && GetToolTipReference(owner) is { } toolTip)
			{
				if (e.GetCurrentPoint(owner).Properties.IsLeftButtonPressed)
				{
					toolTip.IsOpen = false;
					toolTip.CurrentHoverId++;
				}
			}
		}
	}
}
