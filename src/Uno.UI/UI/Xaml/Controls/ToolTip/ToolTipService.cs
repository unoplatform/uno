using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Input;
using Uno.UI;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.Disposables;

namespace Windows.UI.Xaml.Controls
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

		public static object GetToolTip(DependencyObject obj) => obj.GetValue(ToolTipProperty);
		public static void SetToolTip(DependencyObject obj, object value) => obj.SetValue(ToolTipProperty, value);

		#endregion
		#region DependencyProperty: Placement

		public static DependencyProperty PlacementProperty { get; } = DependencyProperty.RegisterAttached(
			"Placement",
			typeof(PlacementMode),
			typeof(ToolTipService),
			new PropertyMetadata(PlacementMode.Top, OnPlacementChanged));

		public static PlacementMode GetPlacement(FrameworkElement obj) => (PlacementMode)obj.GetValue(PlacementProperty);
		public static void SetPlacement(FrameworkElement obj, PlacementMode value) => obj.SetValue(PlacementProperty, value);

		#endregion
		#region DependencyProperty: ToolTipReference

		internal static DependencyProperty ToolTipReferenceProperty { get; } = DependencyProperty.RegisterAttached(
			"ToolTipReference",
			typeof(ToolTip),
			typeof(ToolTipService),
			new PropertyMetadata(default(ToolTip)));

		internal static ToolTip GetToolTipReference(DependencyObject obj) => (ToolTip)obj.GetValue(ToolTipReferenceProperty);
		internal static void SetToolTipReference(DependencyObject obj, ToolTip value) => obj.SetValue(ToolTipReferenceProperty, value);

		#endregion


		private static void OnToolTipChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs e)
		{
			if (!FeatureConfiguration.ToolTip.UseToolTips)
			{
				return; // ToolTips are disabled
			}

			if (!(dependencyobject is FrameworkElement element))
			{
				return;
			}

			if (e.NewValue is null)
			{
				DisposePreviousToolTip();
			}
			else if (e.NewValue is ToolTip newToolTip)
			{
				var previousToolTip = GetToolTipReference(element);

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
				var previousTooltip = GetToolTipReference(element);
				if (previousTooltip != null)
				{
					// update the old tooltip with new content
					previousTooltip.Content = e.NewValue;
				}
				else
				{
					// setup a new tooltip
					previousTooltip = new ToolTip { Content = e.NewValue };
					SetupToolTip(previousTooltip);
				}
			}

			void SetupToolTip(ToolTip tooltip)
			{
				tooltip.Placement = GetPlacement(tooltip);
				tooltip.SetAnchor(GetPlacementTarget(element) ?? element);

				SetToolTipReference(element, tooltip);
				tooltip._pointerEventSubscriptions = SubscribeToEvents(element, tooltip);
			}
			void DisposePreviousToolTip(ToolTip tooltip = null)
			{
				tooltip ??= GetToolTipReference(element);

				SetToolTipReference(element, null);
				tooltip._pointerEventSubscriptions?.Dispose();
				tooltip._pointerEventSubscriptions = null;
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
				SubscribeToPointerEvents(control, null);
			}
			control.Loaded += SubscribeToPointerEvents;
			control.Unloaded += UnsubscribeToPointerEvents;

			void SubscribeToPointerEvents(object sender, RoutedEventArgs e)
			{
				control.PointerEntered += OnPointerEntered;
				control.PointerExited += OnPointerExited;
			}
			void UnsubscribeToPointerEvents(object sender, RoutedEventArgs e)
			{
				control.PointerEntered -= OnPointerEntered;
				control.PointerExited -= OnPointerExited;
			}

			return Disposable.Create(() =>
			{
				control.Loaded -= SubscribeToPointerEvents;
				control.Unloaded -= UnsubscribeToPointerEvents;
				UnsubscribeToPointerEvents(control, null);

				tooltip.IsOpen = false;
				tooltip.CurrentHoverId++;
			});

			// pointer event handlers
			async void OnPointerEntered(object snd, PointerRoutedEventArgs evt)
			{
				await HoverTask(++tooltip.CurrentHoverId).ConfigureAwait(false);
			}
			void OnPointerExited(object snd, PointerRoutedEventArgs evt)
			{
				tooltip.CurrentHoverId++;
				tooltip.IsOpen = false;
			}
			async Task HoverTask(long hoverId)
			{
				await Task.Delay(FeatureConfiguration.ToolTip.ShowDelay).ConfigureAwait(false);
				if (tooltip.CurrentHoverId != hoverId)
				{
					return;
				}

				if (control.IsLoaded)
				{
					tooltip.IsOpen = true;

					await Task.Delay(FeatureConfiguration.ToolTip.ShowDuration).ConfigureAwait(false);
					if (tooltip.CurrentHoverId == hoverId)
					{
						tooltip.IsOpen = false;
					}
				}
			}
		}
	}
}
