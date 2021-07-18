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

		public static object GetToolTip(DependencyObject element) =>
			element.GetValue(ToolTipProperty);

		public static void SetToolTip(global::Windows.UI.Xaml.DependencyObject element, object value) =>
			element.SetValue(ToolTipProperty, value);

		internal static ToolTip GetToolTipObject(DependencyObject obj) =>
			(ToolTip)obj.GetValue(ToolTipObjectProperty);

		internal static void SetToolTipObject(DependencyObject obj, ToolTip value) =>
			obj.SetValue(ToolTipObjectProperty, value);

		internal static DependencyProperty ToolTipObjectProperty { get; } =
			DependencyProperty.RegisterAttached("ToolTipObject", typeof(ToolTip), typeof(ToolTipService), new PropertyMetadata(null));


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

			var newToolTip = args.NewValue as ToolTip;
			var existingToolTip = GetToolTipObject(element);

			// Handle cases where the new value is not a ToolTip instance
			if (newToolTip == null && args.NewValue != null)
			{
				if (existingToolTip != null)
				{
					// Just replace content of the existing ToolTip.
					existingToolTip.Content = args.NewValue;
					return;
				}
				else
				{
					// We need to create a ToolTip object for the custom content.
					newToolTip = new ToolTip { Content = args.NewValue };
				}
			}

			// Remove existing ToolTip.
			if (existingToolTip != null)
			{
				existingToolTip.IsOpen = false;
				SetToolTipObject(element, null);
				element.PointerEntered -= OnPointerEntered;
				element.PointerExited -= OnPointerExited;

				element.Unloaded -= OnOwnerUnloaded;
			}

			// Setup new ToolTip, if provided.
			if (newToolTip != null)
			{
				newToolTip.SetAnchor(element);
				SetToolTipObject(element, newToolTip);

				element.PointerEntered += OnPointerEntered;
				element.PointerExited += OnPointerExited;

				element.Unloaded += OnOwnerUnloaded;
			}
		}

		private static void OnPointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (sender is FrameworkElement owner && GetToolTipObject(owner) is ToolTip toolTip)
			{
				var hoverTask = HoverTask(++toolTip.CurrentHoverId);

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

		private static void OnPointerExited(object sender, PointerRoutedEventArgs evt)
		{
			if (sender is FrameworkElement owner && GetToolTipObject(owner) is ToolTip toolTip)
			{
				toolTip.IsOpen = false;
				toolTip.CurrentHoverId++;
			}
		}

		private static void OnOwnerUnloaded(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement owner && GetToolTipObject(owner) is ToolTip toolTip)
			{
				toolTip.IsOpen = false;
			}
		}
	}
}
