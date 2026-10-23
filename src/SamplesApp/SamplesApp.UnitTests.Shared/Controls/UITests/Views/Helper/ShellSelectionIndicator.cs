#nullable enable

using Microsoft.UI.Xaml;
#if HAS_UNO
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
#endif

namespace Uno.UI.Samples.Helper;

/// <summary>
/// Shows the element (the row's accent pill) while its list item is selected. Uno's ListViewItem template has no
/// selection indicator yet, so on Uno the row template draws it; WinUI's own template already does.
/// </summary>
public partial class ShellSelectionIndicator
{
	public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

	public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

	public static DependencyProperty IsEnabledProperty { get; } =
		DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(ShellSelectionIndicator), new PropertyMetadata(false, OnIsEnabledChanged));

	private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not FrameworkElement element)
		{
			return;
		}

		element.Visibility = Visibility.Collapsed;
#if HAS_UNO
		if (e.NewValue is true)
		{
			element.Loaded += OnLoaded;
			element.Unloaded += OnUnloaded;
		}
		else
		{
			element.Loaded -= OnLoaded;
			element.Unloaded -= OnUnloaded;
			Unsubscribe(element);
		}
#endif
	}

#if HAS_UNO
	private static DependencyProperty SubscriptionProperty { get; } =
		DependencyProperty.RegisterAttached("Subscription", typeof(object), typeof(ShellSelectionIndicator), new PropertyMetadata(null));

	private sealed record Subscription(SelectorItem Item, long Token);

	private static void OnLoaded(object sender, RoutedEventArgs e)
	{
		var element = (FrameworkElement)sender;
		Unsubscribe(element);

		var item = FindItem(element);
		if (item is null)
		{
			return;
		}

		var token = item.RegisterPropertyChangedCallback(SelectorItem.IsSelectedProperty, (_, _) => Sync(element, item));
		element.SetValue(SubscriptionProperty, new Subscription(item, token));
		Sync(element, item);
	}

	private static void OnUnloaded(object sender, RoutedEventArgs e) => Unsubscribe((FrameworkElement)sender);

	private static void Unsubscribe(FrameworkElement element)
	{
		if (element.GetValue(SubscriptionProperty) is Subscription subscription)
		{
			subscription.Item.UnregisterPropertyChangedCallback(SelectorItem.IsSelectedProperty, subscription.Token);
			element.ClearValue(SubscriptionProperty);
		}
	}

	private static void Sync(FrameworkElement element, SelectorItem item)
		=> element.Visibility = item.IsSelected ? Visibility.Visible : Visibility.Collapsed;

	private static SelectorItem? FindItem(DependencyObject element)
	{
		for (var current = VisualTreeHelper.GetParent(element); current is not null; current = VisualTreeHelper.GetParent(current))
		{
			if (current is SelectorItem item)
			{
				return item;
			}
		}

		return null;
	}
#endif
}
