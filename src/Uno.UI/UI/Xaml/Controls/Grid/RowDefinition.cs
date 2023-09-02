using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Uno.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls
{
	[DebuggerDisplay("{DebugDisplay,nq}")]
	[ContentProperty(Name = nameof(Height))]
	public partial class RowDefinition : DefinitionBase, DependencyObject
	{
		public RowDefinition()
		{
			InitializeBinder();
			IsAutoPropertyInheritanceEnabled = false;
		}

		// This method is called from the generated IDependencyObjectInternal.OnPropertyChanged2 method
		internal void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			InvalidateDefinition();
		}

		#region Height DependencyProperty

		private static GridLength GetHeightDefaultValue() => GridLengthHelper.OneStar;

		[GeneratedDependencyProperty]
		public static DependencyProperty HeightProperty { get; } = CreateHeightProperty();

		public GridLength Height
		{
			get => GetHeightValue();
			set => SetHeightValue(value);
		}

		#endregion

		public static implicit operator RowDefinition(string value)
		{
			return new RowDefinition { Height = GridLength.ParseGridLength(value).First() };
		}

		[GeneratedDependencyProperty(DefaultValue = 0d)]
		public static DependencyProperty MinHeightProperty { get; } = CreateMinHeightProperty();

		public double MinHeight
		{
			get => GetMinHeightValue();
			set => SetMinHeightValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = double.PositiveInfinity)]
		public static DependencyProperty MaxHeightProperty { get; } = CreateMaxHeightProperty();
		public double MaxHeight
		{
			get => GetMaxHeightValue();
			set => SetMaxHeightValue(value);
		}

		public double ActualHeight
		{
			get
			{
				//var parent = this.GetParent();
				//var result = (parent as Grid)?.GetActualHeight(this) ?? 0d;
				//return result;
				return _measureArrangeSize;
			}
		}

		//private string DebugDisplay => $"RowDefinition(Height={Height.ToDisplayString()};MinHeight={MinHeight};MaxHeight={MaxHeight};ActualHeight={ActualHeight}";
		private string DebugDisplay => $"RowDefinition(Height={Height.ToDisplayString()};MinHeight={MinHeight};MaxHeight={MaxHeight}";

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

		double DefinitionBase.GetUserSizeValue() => Height.Value;

		GridUnitType DefinitionBase.GetUserSizeType() => Height.GridUnitType;

		double DefinitionBase.GetUserMaxSize() => MaxHeight;

		double DefinitionBase.GetUserMinSize() => MinHeight;

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
