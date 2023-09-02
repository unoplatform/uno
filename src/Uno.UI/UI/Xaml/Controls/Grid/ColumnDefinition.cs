using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls
{
	[DebuggerDisplay("{DebugDisplay,nq}")]
	[ContentProperty(Name = nameof(Width))]
	public partial class ColumnDefinition : DefinitionBase, DependencyObject
	{
		public ColumnDefinition()
		{
			InitializeBinder();
			IsAutoPropertyInheritanceEnabled = false;
		}

		// This method is called from the generated IDependencyObjectInternal.OnPropertyChanged2 method
		internal void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			InvalidateDefinition();
		}

		#region Width DependencyProperty

		private static GridLength GetWidthDefaultValue() => GridLengthHelper.OneStar;

		[GeneratedDependencyProperty]
		public static DependencyProperty WidthProperty { get; } = CreateWidthProperty();

		public GridLength Width
		{
			get => GetWidthValue();
			set => SetWidthValue(value);
		}

		#endregion

		public static implicit operator ColumnDefinition(string value)
		{
			return new ColumnDefinition { Width = GridLength.ParseGridLength(value).First() };
		}

		[GeneratedDependencyProperty(DefaultValue = 0d)]
		public static DependencyProperty MinWidthProperty { get; } = CreateMinWidthProperty();

		public double MinWidth
		{
			get => GetMinWidthValue();
			set => SetMinWidthValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = double.PositiveInfinity)]
		public static DependencyProperty MaxWidthProperty { get; } = CreateMaxWidthProperty();

		public double MaxWidth
		{
			get => GetMaxWidthValue();
			set => SetMaxWidthValue(value);
		}

		public double ActualWidth
		{
			get
			{
				return _measureArrangeSize;
			}
		}

		//private string DebugDisplay => $"ColumnDefinition(Width={Width.ToDisplayString()};MinWidth={MinWidth};MaxWidth={MaxWidth};ActualWidth={ActualWidth}";
		private string DebugDisplay => $"ColumnDefinition(Width={Width.ToDisplayString()};MinWidth={MinWidth};MaxWidth={MaxWidth}";

		#region internal DefinitionBase

		private void InvalidateDefinition()
		{
			if (this.GetParent() is Grid parentGrid)
			{
				parentGrid.InvalidateDefinitions();
			}
		}

		private double _effectiveMinSize;
		private double _measureArrangeSize;
		private double _sizeCache;
		private double _finalOffset;
		private GridUnitType _effectiveUnitType;

		double DefinitionBase.GetUserSizeValue() => Width.Value;

		GridUnitType DefinitionBase.GetUserSizeType() => Width.GridUnitType;

		double DefinitionBase.GetUserMaxSize() => MaxWidth;

		double DefinitionBase.GetUserMinSize() => MinWidth;

		double DefinitionBase.GetEffectiveMinSize() => _effectiveMinSize;

		void DefinitionBase.SetEffectiveMinSize(double value) => _effectiveMinSize = value;

		double DefinitionBase.GetMeasureArrangeSize() => _measureArrangeSize;

		void DefinitionBase.SetMeasureArrangeSize(double value) => _measureArrangeSize = value;

		double DefinitionBase.GetSizeCache() => _sizeCache;

		void DefinitionBase.SetSizeCache(double value) => _sizeCache = value;

		double DefinitionBase.GetFinalOffset() => _finalOffset;

		void DefinitionBase.SetFinalOffset(double value) => _finalOffset = value;

		GridUnitType DefinitionBase.GetEffectiveUnitType() => _effectiveUnitType;

		void DefinitionBase.SetEffectiveUnitType(GridUnitType type) => _effectiveUnitType = type;

		double DefinitionBase.GetPreferredSize()
		{
			return
				(_effectiveUnitType != GridUnitType.Auto
				 && _effectiveMinSize < _measureArrangeSize)
					? _measureArrangeSize
					: _effectiveMinSize;
		}

		void DefinitionBase.UpdateEffectiveMinSize(double newValue)
		{
			_effectiveMinSize = Math.Max(_effectiveMinSize, newValue);
		}

		#endregion
	}
}
