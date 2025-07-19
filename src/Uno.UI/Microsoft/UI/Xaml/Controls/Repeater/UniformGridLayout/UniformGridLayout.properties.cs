// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable


using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	partial class UniformGridLayout
	{
		public static DependencyProperty ItemsJustificationProperty { get; } = DependencyProperty.Register(
			"ItemsJustification", typeof(UniformGridLayoutItemsJustification), typeof(UniformGridLayout),
			new FrameworkPropertyMetadata(UniformGridLayoutItemsJustification.Start, propertyChangedCallback: (sender, args) => ((UniformGridLayout)sender).OnPropertyChanged(args)));

		public UniformGridLayoutItemsJustification ItemsJustification
		{
			get => (UniformGridLayoutItemsJustification)GetValue(ItemsJustificationProperty);
			set => SetValue(ItemsJustificationProperty, value);
		}

		public static DependencyProperty ItemsStretchProperty { get; } = DependencyProperty.Register(
			"ItemsStretch", typeof(UniformGridLayoutItemsStretch), typeof(UniformGridLayout),
			new FrameworkPropertyMetadata(UniformGridLayoutItemsStretch.None, propertyChangedCallback: (sender, args) => ((UniformGridLayout)sender).OnPropertyChanged(args)));

		public UniformGridLayoutItemsStretch ItemsStretch
		{
			get => (UniformGridLayoutItemsStretch)GetValue(ItemsStretchProperty);
			set => SetValue(ItemsStretchProperty, value);
		}

		public static DependencyProperty MaximumRowsOrColumnsProperty { get; } = DependencyProperty.Register(
			"MaximumRowsOrColumns", typeof(int), typeof(UniformGridLayout), new FrameworkPropertyMetadata(-1, propertyChangedCallback: (sender, args) => ((UniformGridLayout)sender).OnPropertyChanged(args)));

		public int MaximumRowsOrColumns
		{
			get => (int)GetValue(MaximumRowsOrColumnsProperty);
			set => SetValue(MaximumRowsOrColumnsProperty, value);
		}

		public static DependencyProperty MinColumnSpacingProperty { get; } = DependencyProperty.Register(
			"MinColumnSpacing", typeof(double), typeof(UniformGridLayout), new FrameworkPropertyMetadata(0d, propertyChangedCallback: (sender, args) => ((UniformGridLayout)sender).OnPropertyChanged(args)));

		public double MinColumnSpacing
		{
			get => (double)GetValue(MinColumnSpacingProperty);
			set => SetValue(MinColumnSpacingProperty, value);
		}

		public static DependencyProperty MinItemHeightProperty { get; } = DependencyProperty.Register(
			"MinItemHeight", typeof(double), typeof(UniformGridLayout), new FrameworkPropertyMetadata(0d, propertyChangedCallback: (sender, args) => ((UniformGridLayout)sender).OnPropertyChanged(args)));

		public double MinItemHeight
		{
			get => (double)GetValue(MinItemHeightProperty);
			set => SetValue(MinItemHeightProperty, value);
		}

		public static DependencyProperty MinItemWidthProperty { get; } = DependencyProperty.Register(
			"MinItemWidth", typeof(double), typeof(UniformGridLayout), new FrameworkPropertyMetadata(0d, propertyChangedCallback: (sender, args) => ((UniformGridLayout)sender).OnPropertyChanged(args)));

		public double MinItemWidth
		{
			get => (double)GetValue(MinItemWidthProperty);
			set => SetValue(MinItemWidthProperty, value);
		}

		public static DependencyProperty MinRowSpacingProperty { get; } = DependencyProperty.Register(
			"MinRowSpacing", typeof(double), typeof(UniformGridLayout), new FrameworkPropertyMetadata(0d, propertyChangedCallback: (sender, args) => ((UniformGridLayout)sender).OnPropertyChanged(args)));

		public double MinRowSpacing
		{
			get => (double)GetValue(MinRowSpacingProperty);
			set => SetValue(MinRowSpacingProperty, value);
		}

		public static DependencyProperty OrientationProperty { get; } = DependencyProperty.Register(
			"Orientation", typeof(Orientation), typeof(UniformGridLayout),
			new FrameworkPropertyMetadata(Orientation.Horizontal, propertyChangedCallback: (sender, args) => ((UniformGridLayout)sender).OnPropertyChanged(args)));

		public Orientation Orientation
		{
			get => (Orientation)GetValue(OrientationProperty);
			set => SetValue(OrientationProperty, value);
		}



		//GlobalDependencyProperty s_ItemsJustificationProperty{ null };
		//GlobalDependencyProperty s_ItemsStretchProperty{ null };
		//GlobalDependencyProperty s_MaximumRowsOrColumnsProperty{ null };
		//GlobalDependencyProperty s_MinColumnSpacingProperty{ null };
		//GlobalDependencyProperty s_MinItemHeightProperty{ null };
		//GlobalDependencyProperty s_MinItemWidthProperty{ null };
		//GlobalDependencyProperty s_MinRowSpacingProperty{ null };
		//GlobalDependencyProperty s_OrientationProperty{ null };

		//UniformGridLayoutProperties()
		//{
		//    EnsureProperties();
		//}


		//void EnsureProperties()
		//{
		//    if (!s_ItemsJustificationProperty)
		//    {
		//        s_ItemsJustificationProperty =
		//            InitializeDependencyProperty(
		//                "ItemsJustification",
		//                name_of<UniformGridLayoutItemsJustification>(),
		//                name_of<UniformGridLayout>(),
		//                false /* isAttached */,
		//                ValueHelper<UniformGridLayoutItemsJustification>.BoxValueIfNecessary(UniformGridLayoutItemsJustification.Start),
		//                PropertyChangedCallback(OnItemsJustificationPropertyChanged));
		//    }
		//    if (!s_ItemsStretchProperty)
		//    {
		//        s_ItemsStretchProperty =
		//            InitializeDependencyProperty(
		//                "ItemsStretch",
		//                name_of<UniformGridLayoutItemsStretch>(),
		//                name_of<UniformGridLayout>(),
		//                false /* isAttached */,
		//                ValueHelper<UniformGridLayoutItemsStretch>.BoxValueIfNecessary(UniformGridLayoutItemsStretch.None),
		//                PropertyChangedCallback(OnItemsStretchPropertyChanged));
		//    }
		//    if (!s_MaximumRowsOrColumnsProperty)
		//    {
		//        s_MaximumRowsOrColumnsProperty =
		//            InitializeDependencyProperty(
		//                "MaximumRowsOrColumns",
		//                name_of<int>(),
		//                name_of<UniformGridLayout>(),
		//                false /* isAttached */,
		//                ValueHelper<int>.BoxValueIfNecessary(-1),
		//                PropertyChangedCallback(OnMaximumRowsOrColumnsPropertyChanged));
		//    }
		//    if (!s_MinColumnSpacingProperty)
		//    {
		//        s_MinColumnSpacingProperty =
		//            InitializeDependencyProperty(
		//                "MinColumnSpacing",
		//                name_of<double>(),
		//                name_of<UniformGridLayout>(),
		//                false /* isAttached */,
		//                ValueHelper<double>.BoxValueIfNecessary(0.0),
		//                PropertyChangedCallback(OnMinColumnSpacingPropertyChanged));
		//    }
		//    if (!s_MinItemHeightProperty)
		//    {
		//        s_MinItemHeightProperty =
		//            InitializeDependencyProperty(
		//                "MinItemHeight",
		//                name_of<double>(),
		//                name_of<UniformGridLayout>(),
		//                false /* isAttached */,
		//                ValueHelper<double>.BoxValueIfNecessary(0.0),
		//                PropertyChangedCallback(OnMinItemHeightPropertyChanged));
		//    }
		//    if (!s_MinItemWidthProperty)
		//    {
		//        s_MinItemWidthProperty =
		//            InitializeDependencyProperty(
		//                "MinItemWidth",
		//                name_of<double>(),
		//                name_of<UniformGridLayout>(),
		//                false /* isAttached */,
		//                ValueHelper<double>.BoxValueIfNecessary(0.0),
		//                PropertyChangedCallback(OnMinItemWidthPropertyChanged));
		//    }
		//    if (!s_MinRowSpacingProperty)
		//    {
		//        s_MinRowSpacingProperty =
		//            InitializeDependencyProperty(
		//                "MinRowSpacing",
		//                name_of<double>(),
		//                name_of<UniformGridLayout>(),
		//                false /* isAttached */,
		//                ValueHelper<double>.BoxValueIfNecessary(0.0),
		//                PropertyChangedCallback(OnMinRowSpacingPropertyChanged));
		//    }
		//    if (!s_OrientationProperty)
		//    {
		//        s_OrientationProperty =
		//            InitializeDependencyProperty(
		//                "Orientation",
		//                name_of<Orientation>(),
		//                name_of<UniformGridLayout>(),
		//                false /* isAttached */,
		//                ValueHelper<Orientation>.BoxValueIfNecessary(Orientation.Horizontal),
		//                PropertyChangedCallback(OnOrientationPropertyChanged));
		//    }
		//}

		//void ClearProperties()
		//{
		//    s_ItemsJustificationProperty = null;
		//    s_ItemsStretchProperty = null;
		//    s_MaximumRowsOrColumnsProperty = null;
		//    s_MinColumnSpacingProperty = null;
		//    s_MinItemHeightProperty = null;
		//    s_MinItemWidthProperty = null;
		//    s_MinRowSpacingProperty = null;
		//    s_OrientationProperty = null;
		//}

		//		void OnItemsJustificationPropertyChanged(
		//	DependencyObject sender,
		//	DependencyPropertyChangedEventArgs args)
		//{
		//(    var owner = sender as UniformGridLayout);
		//	get_self<UniformGridLayout>(owner).OnPropertyChanged(args);
		//}

		//void OnItemsStretchPropertyChanged(
		//	DependencyObject sender,
		//	DependencyPropertyChangedEventArgs args)
		//{
		//(    var owner = sender as UniformGridLayout);
		//	get_self<UniformGridLayout>(owner).OnPropertyChanged(args);
		//}

		//void OnMaximumRowsOrColumnsPropertyChanged(
		//	DependencyObject sender,
		//	DependencyPropertyChangedEventArgs args)
		//{
		//(    var owner = sender as UniformGridLayout);
		//	get_self<UniformGridLayout>(owner).OnPropertyChanged(args);
		//}

		//void OnMinColumnSpacingPropertyChanged(
		//	DependencyObject sender,
		//	DependencyPropertyChangedEventArgs args)
		//{
		//(    var owner = sender as UniformGridLayout);
		//	get_self<UniformGridLayout>(owner).OnPropertyChanged(args);
		//}

		//void OnMinItemHeightPropertyChanged(
		//	DependencyObject sender,
		//	DependencyPropertyChangedEventArgs args)
		//{
		//(    var owner = sender as UniformGridLayout);
		//	get_self<UniformGridLayout>(owner).OnPropertyChanged(args);
		//}

		//void OnMinItemWidthPropertyChanged(
		//	DependencyObject sender,
		//	DependencyPropertyChangedEventArgs args)
		//{
		//(    var owner = sender as UniformGridLayout);
		//	get_self<UniformGridLayout>(owner).OnPropertyChanged(args);
		//}

		//void OnMinRowSpacingPropertyChanged(
		//	DependencyObject sender,
		//	DependencyPropertyChangedEventArgs args)
		//{
		//(    var owner = sender as UniformGridLayout);
		//	get_self<UniformGridLayout>(owner).OnPropertyChanged(args);
		//}

		//void OnOrientationPropertyChanged(
		//	DependencyObject sender,
		//	DependencyPropertyChangedEventArgs args)
		//{
		//(    var owner = sender as UniformGridLayout);
		//	get_self<UniformGridLayout>(owner).OnPropertyChanged(args);
		//}

		//void ItemsJustification(UniformGridLayoutItemsJustification value)
		//{
		//[[gsl.suppress(con)]]
		//{
		//	(UniformGridLayout)(this).SetValue(s_ItemsJustificationProperty,
		//		ValueHelper<UniformGridLayoutItemsJustification>.BoxValueIfNecessary(value));
		//}
		//}

		//UniformGridLayoutItemsJustification ItemsJustification() => ValueHelper<UniformGridLayoutItemsJustification>.
		//CastOrUnbox((UniformGridLayout)(this).GetValue(s_ItemsJustificationProperty));

		//void ItemsStretch(UniformGridLayoutItemsStretch value)
		//{
		//[[gsl.suppress(con)]]
		//{
		//	(UniformGridLayout)(this).SetValue(s_ItemsStretchProperty,
		//		ValueHelper<UniformGridLayoutItemsStretch>.BoxValueIfNecessary(value));
		//}
		//}

		//UniformGridLayoutItemsStretch ItemsStretch() => ValueHelper<UniformGridLayoutItemsStretch>.CastOrUnbox((
		//UniformGridLayout)(this).GetValue(s_ItemsStretchProperty));

		//void MaximumRowsOrColumns(int value)
		//{
		//[[gsl.suppress(con)]]
		//{
		//	(UniformGridLayout)(this).SetValue(s_MaximumRowsOrColumnsProperty, ValueHelper<int>.BoxValueIfNecessary(value));
		//}
		//}

		//int MaximumRowsOrColumns() => ValueHelper<int>.CastOrUnbox((UniformGridLayout)(this).GetValue(
		//s_MaximumRowsOrColumnsProperty));

		//void MinColumnSpacing(double value)
		//{
		//[[gsl.suppress(con)]]
		//{
		//	(UniformGridLayout)(this).SetValue(s_MinColumnSpacingProperty, ValueHelper<double>.BoxValueIfNecessary(value));
		//}
		//}

		//double MinColumnSpacing() => ValueHelper<double>.CastOrUnbox((UniformGridLayout)(this).GetValue(
		//s_MinColumnSpacingProperty));

		//void MinItemHeight(double value)
		//{
		//[[gsl.suppress(con)]]
		//{
		//	(UniformGridLayout)(this).SetValue(s_MinItemHeightProperty, ValueHelper<double>.BoxValueIfNecessary(value));
		//}
		//}

		//double MinItemHeight() => ValueHelper<double>.CastOrUnbox((UniformGridLayout)(this).GetValue(s_MinItemHeightProperty
		//));

		//void MinItemWidth(double value)
		//{
		//[[gsl.suppress(con)]]
		//{
		//	(UniformGridLayout)(this).SetValue(s_MinItemWidthProperty, ValueHelper<double>.BoxValueIfNecessary(value));
		//}
		//}

		//double MinItemWidth() => ValueHelper<double>.CastOrUnbox((UniformGridLayout)(this).GetValue(s_MinItemWidthProperty))
		//;

		//void MinRowSpacing(double value)
		//{
		//[[gsl.suppress(con)]]
		//{
		//	(UniformGridLayout)(this).SetValue(s_MinRowSpacingProperty, ValueHelper<double>.BoxValueIfNecessary(value));
		//}
		//}

		//double MinRowSpacing() => ValueHelper<double>.CastOrUnbox((UniformGridLayout)(this).GetValue(s_MinRowSpacingProperty
		//));

		//void Orientation(Orientation value)
		//{
		//[[gsl.suppress(con)]]
		//{
		//	(UniformGridLayout)(this).SetValue(s_OrientationProperty, ValueHelper<Orientation>.BoxValueIfNecessary(value));
		//}
		//}

		//Orientation Orientation() => ValueHelper<Orientation>.CastOrUnbox((UniformGridLayout)(this).GetValue(
		//s_OrientationProperty));
	}
}
