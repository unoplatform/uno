#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Uno.Diagnostics.UI;

internal static class RelativePlacement
{
	private static readonly ConditionalWeakTable<FrameworkElement, List<FrameworkElement>> _targets = new();

	#region Anchor (Attached DP)
	public static DependencyProperty AnchorProperty { get; } = DependencyProperty.RegisterAttached(
		"Anchor",
		typeof(FrameworkElement),
		typeof(RelativePlacement),
		new PropertyMetadata(default(FrameworkElement), OnAnchorChanged));

	public static FrameworkElement? GetAnchor(UIElement elt)
		=> (FrameworkElement)elt.GetValue(AnchorProperty);

	public static void SetAnchor(UIElement elt, FrameworkElement? target)
		=> elt.SetValue(AnchorProperty, target);

	private static void OnAnchorChanged(DependencyObject elt, DependencyPropertyChangedEventArgs args)
	{
		if (elt is not FrameworkElement target)
		{
			return;
		}

		target.SizeChanged -= OnTargetSizeChanged;
		if (args.OldValue is FrameworkElement oldAnchor)
		{
			oldAnchor.SizeChanged -= OnAnchorSizeChanged;
		}

		if (args.NewValue is FrameworkElement newAnchor)
		{
			target.SizeChanged += OnTargetSizeChanged;
			newAnchor.SizeChanged += OnAnchorSizeChanged;
		}
		else
		{
			target.RenderTransform = null;
		}
	}
	#endregion

	#region Mode (Attached DP)
	public static DependencyProperty ModeProperty { get; } = DependencyProperty.RegisterAttached(
		"Mode",
		typeof(FlyoutPlacementMode),
		typeof(RelativePlacement),
		new PropertyMetadata(FlyoutPlacementMode.Right, OnModeChanged));

	public static FlyoutPlacementMode GetMode(UIElement elt)
		=> (FlyoutPlacementMode)elt.GetValue(ModeProperty);

	public static void SetMode(UIElement elt, FlyoutPlacementMode mode)
		=> elt.SetValue(ModeProperty, mode);

	private static void OnModeChanged(DependencyObject elt, DependencyPropertyChangedEventArgs args)
	{
		if (elt is not FrameworkElement element)
		{
			return;
		}

		if (GetAnchor(element) is { } anchor)
		{
			RefreshPlacement(anchor, element);
		}
	}
	#endregion

	private static void OnTargetSizeChanged(object sender, SizeChangedEventArgs args)
	{
		if (sender is FrameworkElement target && GetAnchor(target) is { } anchor)
		{
			RefreshPlacement(anchor, target);
		}
	}

	private static void OnAnchorSizeChanged(object sender, SizeChangedEventArgs args)
	{
		if (sender is FrameworkElement anchor)
		{
			RefreshPlacement(anchor);
		}
	}

	private static void RefreshPlacement(FrameworkElement anchor)
	{
		if (!_targets.TryGetValue(anchor, out var targets) || targets is null or { Count: 0 })
		{
			return;
		}

		foreach (var target in targets)
		{
			RefreshPlacement(anchor, target);
		}
	}

	private static void RefreshPlacement(FrameworkElement anchor, FrameworkElement target)
	{
		var mode = GetMode(target);
		var targetLocation = mode switch
		{
			FlyoutPlacementMode.Left => new Point(-target.ActualWidth, 0),
			FlyoutPlacementMode.Right => new Point(anchor.ActualWidth, 0),
			_ => throw new NotImplementedException("Only Left and Right mode are currently supported"),
		};

		if (target.RenderTransform is not TranslateTransform transform)
		{
			target.RenderTransform = transform = new();
		}

		// TODO: This assumes that target is at same origin that the anchor, we need to TransformToVisual to get the correct position
		transform.X = targetLocation.X;
		transform.Y = targetLocation.Y;
	}
}
