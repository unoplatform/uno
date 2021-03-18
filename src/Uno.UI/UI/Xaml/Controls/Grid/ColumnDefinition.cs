using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml.Controls
{
	[DebuggerDisplay("{DebugDisplay,nq}")]
	public partial class ColumnDefinition : DependencyObject
	{
		public ColumnDefinition()
		{
			InitializeBinder();
			IsAutoPropertyInheritanceEnabled = false;
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

		private static GridLength GetMaxWidthDefaultValue() => GridLengthHelper.OneStar;

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
				var parent = this.GetParent();
				var result = (parent as Grid)?.GetActualWidth(this) ?? 0d;
				return result;
			}
		}

		private string DebugDisplay => $"ColumnDefinition(Width={Width.ToDisplayString()};MinWidth={MinWidth};MaxWidth={MaxWidth};ActualWidth={ActualWidth}";
	}
}
