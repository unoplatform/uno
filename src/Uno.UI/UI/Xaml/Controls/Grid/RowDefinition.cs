using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml.Controls
{
	[DebuggerDisplay("{DebugDisplay,nq}")]
	public partial class RowDefinition : DependencyObject
	{
		public RowDefinition()
		{
			InitializeBinder();
			IsAutoPropertyInheritanceEnabled = false;
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

		private static GridLength GetMaxHeightDefaultValue() => GridLengthHelper.OneStar;

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
				var parent = this.GetParent();
				var result = (parent as Grid)?.GetActualHeight(this) ?? 0d;
				return result;
			}
		}

		private string DebugDisplay => $"RowDefinition(Height={Height.ToDisplayString()};MinHeight={MinHeight};MaxHeight={MaxHeight};ActualHeight={ActualHeight}";
	}
}
