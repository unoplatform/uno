// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayout.properties.cpp, commit b8cfb8490

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class LinedFlowLayout
	{
		public static DependencyProperty ActualLineHeightProperty { get; } = DependencyProperty.Register(
			nameof(ActualLineHeight), typeof(double), typeof(LinedFlowLayout),
			new FrameworkPropertyMetadata(s_defaultActualLineHeight, propertyChangedCallback: (sender, args) => ((LinedFlowLayout)sender).OnPropertyChanged(args)));

		public double ActualLineHeight
		{
			get => (double)GetValue(ActualLineHeightProperty);
			internal set => SetValue(ActualLineHeightProperty, value);
		}

		public static DependencyProperty ItemsJustificationProperty { get; } = DependencyProperty.Register(
			nameof(ItemsJustification), typeof(LinedFlowLayoutItemsJustification), typeof(LinedFlowLayout),
			new FrameworkPropertyMetadata(s_defaultItemsJustification, propertyChangedCallback: (sender, args) => ((LinedFlowLayout)sender).OnPropertyChanged(args)));

		public LinedFlowLayoutItemsJustification ItemsJustification
		{
			get => (LinedFlowLayoutItemsJustification)GetValue(ItemsJustificationProperty);
			set => SetValue(ItemsJustificationProperty, value);
		}

		public static DependencyProperty ItemsStretchProperty { get; } = DependencyProperty.Register(
			nameof(ItemsStretch), typeof(LinedFlowLayoutItemsStretch), typeof(LinedFlowLayout),
			new FrameworkPropertyMetadata(s_defaultItemsStretch, propertyChangedCallback: (sender, args) => ((LinedFlowLayout)sender).OnPropertyChanged(args)));

		public LinedFlowLayoutItemsStretch ItemsStretch
		{
			get => (LinedFlowLayoutItemsStretch)GetValue(ItemsStretchProperty);
			set => SetValue(ItemsStretchProperty, value);
		}

		public static DependencyProperty LineHeightProperty { get; } = DependencyProperty.Register(
			nameof(LineHeight), typeof(double), typeof(LinedFlowLayout),
			new FrameworkPropertyMetadata(s_defaultLineHeight, propertyChangedCallback: (sender, args) => ((LinedFlowLayout)sender).OnPropertyChanged(args)));

		public double LineHeight
		{
			get => (double)GetValue(LineHeightProperty);
			set => SetValue(LineHeightProperty, value);
		}

		public static DependencyProperty LineSpacingProperty { get; } = DependencyProperty.Register(
			nameof(LineSpacing), typeof(double), typeof(LinedFlowLayout),
			new FrameworkPropertyMetadata(s_defaultLineSpacing, propertyChangedCallback: (sender, args) => ((LinedFlowLayout)sender).OnPropertyChanged(args)));

		public double LineSpacing
		{
			get => (double)GetValue(LineSpacingProperty);
			set => SetValue(LineSpacingProperty, value);
		}

		public static DependencyProperty MinItemSpacingProperty { get; } = DependencyProperty.Register(
			nameof(MinItemSpacing), typeof(double), typeof(LinedFlowLayout),
			new FrameworkPropertyMetadata(s_defaultMinItemSpacing, propertyChangedCallback: (sender, args) => ((LinedFlowLayout)sender).OnPropertyChanged(args)));

		public double MinItemSpacing
		{
			get => (double)GetValue(MinItemSpacingProperty);
			set => SetValue(MinItemSpacingProperty, value);
		}

#pragma warning disable 67 // Events raised from the measure fast/regular path in WS-D3.
		public event TypedEventHandler<LinedFlowLayout, LinedFlowLayoutItemsInfoRequestedEventArgs>? ItemsInfoRequested;
		public event TypedEventHandler<LinedFlowLayout, object>? ItemsUnlocked;
#pragma warning restore 67
	}
}
