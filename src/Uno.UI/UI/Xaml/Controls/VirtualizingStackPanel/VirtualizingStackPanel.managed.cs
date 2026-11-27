using System;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

// Uno specific: WinUI's VirtualizingStackPanel layout is not ported. Under a Selector (e.g. the default ListBox template),
// the panel borrows the ItemsStackPanel virtualization, like CarouselPanel. Under a plain ItemsControl, which adds the item
// containers itself, it stacks them without virtualizing, as WinUI does.
public partial class VirtualizingStackPanel
{
	// Created on first use, so that a panel which only stacks never hooks the virtualization to its ScrollViewer.
	private ItemsStackPanelLayout _layout;

	public VirtualizingStackPanel()
	{
		Loading += OnVirtualizingStackPanelLoading;
	}

	/// <summary>
	/// Gets or sets a value that describes the horizontal or vertical orientation of stacked content.
	/// </summary>
	public Orientation Orientation
	{
		get => GetOrientationValue();
		set => SetOrientationValue(value);
	}

	/// <summary>
	/// Identifies the Orientation dependency property.
	/// </summary>
	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
	public static DependencyProperty OrientationProperty { get; } = CreateOrientationProperty();

	private static Orientation GetOrientationDefaultValue() => Orientation.Vertical;

	private bool IsVirtualizingLayout => ItemsOwner is { ManagesItemsPanelChildren: false };

	internal override Orientation? PhysicalOrientation => Orientation;

	protected override Size MeasureOverride(Size availableSize)
		=> IsVirtualizingLayout ? GetOrCreateLayout().MeasureOverride(availableSize) : MeasureStack(availableSize);

	protected override Size ArrangeOverride(Size finalSize)
		=> IsVirtualizingLayout ? GetOrCreateLayout().ArrangeOverride(finalSize) : ArrangeStack(finalSize);

	private protected override VirtualizingPanelLayout GetLayouterCore() => GetOrCreateLayout();

	private ItemsStackPanelLayout GetOrCreateLayout(bool isLoading = false)
	{
		if (_layout is null)
		{
			_layout = new ItemsStackPanelLayout();
			_layout.BindToEquivalentProperty(this, nameof(Orientation));

			if (isLoading)
			{
				_layout.InitializeWhileLoading(this);
			}
			else
			{
				_layout.Initialize(this);
			}
		}

		return _layout;
	}

	// The layout hooks itself to the ScrollViewer and the ItemsControl on Loading, so it must exist by then.
	private void OnVirtualizingStackPanelLoading(FrameworkElement sender, object args)
	{
		if (_layout is null && IsVirtualizingLayout)
		{
			GetOrCreateLayout(isLoading: true);
		}
	}

	private bool _stackLogged;

	private Size MeasureStack(Size availableSize)
	{
		if (!_stackLogged && this.Log().IsEnabled(LogLevel.Debug))
		{
			_stackLogged = true;
			this.Log().Debug($"Stacking the items without virtualizing: {ItemsOwner?.GetType().Name ?? "no ItemsControl"} manages the children.");
		}

		var isHorizontal = Orientation == Orientation.Horizontal;
		var childAvailableSize = isHorizontal
			? new Size(double.PositiveInfinity, availableSize.Height)
			: new Size(availableSize.Width, double.PositiveInfinity);

		double stacked = 0, across = 0;
		foreach (var child in Children)
		{
			child.Measure(childAvailableSize);
			var desired = child.DesiredSize;
			stacked += isHorizontal ? desired.Width : desired.Height;
			across = Math.Max(across, isHorizontal ? desired.Height : desired.Width);
		}

		return isHorizontal ? new Size(stacked, across) : new Size(across, stacked);
	}

	private Size ArrangeStack(Size finalSize)
	{
		var isHorizontal = Orientation == Orientation.Horizontal;

		double offset = 0;
		foreach (var child in Children)
		{
			var desired = child.DesiredSize;
			child.Arrange(isHorizontal
				? new Rect(offset, 0, desired.Width, Math.Max(finalSize.Height, desired.Height))
				: new Rect(0, offset, Math.Max(finalSize.Width, desired.Width), desired.Height));
			offset += isHorizontal ? desired.Width : desired.Height;
		}

		return finalSize;
	}
}
