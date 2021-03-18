using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Input;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	partial class ToolTipService
	{
		public static DependencyProperty ToolTipProperty { get; } =
			DependencyProperty.RegisterAttached(
				"ToolTip",
				typeof(object),
				typeof(ToolTipService),
				new FrameworkPropertyMetadata(default, OnToolTipChanged));

		public static object GetToolTip(DependencyObject element)
		{
			return element.GetValue(ToolTipProperty);
		}

		public static void SetToolTip( global::Windows.UI.Xaml.DependencyObject element, object value)
		{
			element.SetValue(ToolTipProperty, value);
		}

		private static void OnToolTipChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
		{
			if (!FeatureConfiguration.ToolTip.UseToolTips)
			{
				return; // ToolTips are disabled
			}

			if (!(dependencyobject is FrameworkElement element))
			{
				return;
			}

			var toolTip = args.NewValue as ToolTip;

			if (toolTip == null && args.NewValue != null)
			{
				toolTip = new ToolTip { Content = args.NewValue };
			}

			if (toolTip != null)
			{
				// First time: we're subscribing to event handlers

				long currentHoverId = 0;

				toolTip.SetAnchor(element);

				element.Loaded += (snd, evt) =>
				{
					element.PointerEntered += OnPointerEntered;
					element.PointerExited += OnPointerExited;
				};

				element.Unloaded += (snd, evt) =>
				{
					toolTip.IsOpen = false;

					element.PointerEntered -= OnPointerEntered;
					element.PointerExited -= OnPointerExited;
				};

				void OnPointerEntered(object snd, PointerRoutedEventArgs evt)
				{
					var t = HoverTask(++currentHoverId);
				}

				void OnPointerExited(object snd, PointerRoutedEventArgs evt)
				{
					currentHoverId++;
					toolTip.IsOpen = false;
				}

				async Task HoverTask(long hoverId)
				{
					await Task.Delay(FeatureConfiguration.ToolTip.ShowDelay);
					if (currentHoverId != hoverId)
					{
						return;
					}

					if (element.IsLoaded)
					{
						toolTip.IsOpen = true;
						await Task.Delay(FeatureConfiguration.ToolTip.ShowDuration);
						if (currentHoverId == hoverId)
						{
							toolTip.IsOpen = false;
						}
					}
				}
			}
		}
	}
}
