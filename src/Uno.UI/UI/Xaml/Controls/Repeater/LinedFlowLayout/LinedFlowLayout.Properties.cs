// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayout.properties.cpp, commit b8cfb8490

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	partial class LinedFlowLayout
	{
		/// <summary>
		/// Identifies the <see cref="ActualLineHeight"/> dependency property.
		/// </summary>
		public static DependencyProperty ActualLineHeightProperty { get; } = DependencyProperty.Register(
			nameof(ActualLineHeight), typeof(double), typeof(LinedFlowLayout),
			new FrameworkPropertyMetadata(s_defaultActualLineHeight, propertyChangedCallback: (sender, args) => ((LinedFlowLayout)sender).OnPropertyChanged(args)));

		/// <summary>
		/// Gets the actual height used to arrange each line.
		/// </summary>
		public double ActualLineHeight
		{
			get => (double)GetValue(ActualLineHeightProperty);
			internal set => SetValue(ActualLineHeightProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="ItemsJustification"/> dependency property.
		/// </summary>
		public static DependencyProperty ItemsJustificationProperty { get; } = DependencyProperty.Register(
			nameof(ItemsJustification), typeof(LinedFlowLayoutItemsJustification), typeof(LinedFlowLayout),
			new FrameworkPropertyMetadata(s_defaultItemsJustification, propertyChangedCallback: (sender, args) => ((LinedFlowLayout)sender).OnPropertyChanged(args)));

		/// <summary>
		/// Gets or sets how items are aligned along each line.
		/// </summary>
		public LinedFlowLayoutItemsJustification ItemsJustification
		{
			get => (LinedFlowLayoutItemsJustification)GetValue(ItemsJustificationProperty);
			set => SetValue(ItemsJustificationProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="ItemsStretch"/> dependency property.
		/// </summary>
		public static DependencyProperty ItemsStretchProperty { get; } = DependencyProperty.Register(
			nameof(ItemsStretch), typeof(LinedFlowLayoutItemsStretch), typeof(LinedFlowLayout),
			new FrameworkPropertyMetadata(s_defaultItemsStretch, propertyChangedCallback: (sender, args) => ((LinedFlowLayout)sender).OnPropertyChanged(args)));

		/// <summary>
		/// Gets or sets how items are stretched to fill each line.
		/// </summary>
		public LinedFlowLayoutItemsStretch ItemsStretch
		{
			get => (LinedFlowLayoutItemsStretch)GetValue(ItemsStretchProperty);
			set => SetValue(ItemsStretchProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="LineHeight"/> dependency property.
		/// </summary>
		public static DependencyProperty LineHeightProperty { get; } = DependencyProperty.Register(
			nameof(LineHeight), typeof(double), typeof(LinedFlowLayout),
			new FrameworkPropertyMetadata(s_defaultLineHeight, propertyChangedCallback: (sender, args) => ((LinedFlowLayout)sender).OnPropertyChanged(args)));

		/// <summary>
		/// Gets or sets the requested height of each line.
		/// </summary>
		public double LineHeight
		{
			get => (double)GetValue(LineHeightProperty);
			set => SetValue(LineHeightProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="LineSpacing"/> dependency property.
		/// </summary>
		public static DependencyProperty LineSpacingProperty { get; } = DependencyProperty.Register(
			nameof(LineSpacing), typeof(double), typeof(LinedFlowLayout),
			new FrameworkPropertyMetadata(s_defaultLineSpacing, propertyChangedCallback: (sender, args) => ((LinedFlowLayout)sender).OnPropertyChanged(args)));

		/// <summary>
		/// Gets or sets the spacing between lines.
		/// </summary>
		public double LineSpacing
		{
			get => (double)GetValue(LineSpacingProperty);
			set => SetValue(LineSpacingProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="MinItemSpacing"/> dependency property.
		/// </summary>
		public static DependencyProperty MinItemSpacingProperty { get; } = DependencyProperty.Register(
			nameof(MinItemSpacing), typeof(double), typeof(LinedFlowLayout),
			new FrameworkPropertyMetadata(s_defaultMinItemSpacing, propertyChangedCallback: (sender, args) => ((LinedFlowLayout)sender).OnPropertyChanged(args)));

		/// <summary>
		/// Gets or sets the minimum spacing between items in a line.
		/// </summary>
		public double MinItemSpacing
		{
			get => (double)GetValue(MinItemSpacingProperty);
			set => SetValue(MinItemSpacingProperty, value);
		}

		/// <summary>
		/// Occurs when the layout requests sizing information for a range of items.
		/// </summary>
		public event TypedEventHandler<LinedFlowLayout, LinedFlowLayoutItemsInfoRequestedEventArgs>? ItemsInfoRequested;

		/// <summary>
		/// Occurs when items previously locked to lines are unlocked.
		/// </summary>
		public event TypedEventHandler<LinedFlowLayout, object>? ItemsUnlocked;
	}
}
