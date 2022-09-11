using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using CommandBar = Windows.UI.Xaml.Controls.CommandBar;

namespace Windows.UI.Xaml.Controls
{
	partial class AppBarElementContainer
	{
		#region IsCompat

		public bool IsCompact
		{
			get => (bool)this.GetValue(IsCompactProperty);
			set => this.SetValue(IsCompactProperty, value);
		}

		public static DependencyProperty IsCompactProperty { get; } =
			DependencyProperty.Register(
				"IsCompact",
				typeof(bool),
				typeof(AppBarElementContainer),
				new FrameworkPropertyMetadata(default(bool))
			);

		#endregion

		#region DynamicOverflowOrder

		public int DynamicOverflowOrder
		{
			get => (int)this.GetValue(DynamicOverflowOrderProperty);
			set => this.SetValue(DynamicOverflowOrderProperty, value);
		}

		public static DependencyProperty DynamicOverflowOrderProperty { get; } =
			DependencyProperty.Register(
				"DynamicOverflowOrder",
				typeof(int),
				typeof(AppBarElementContainer),
				new FrameworkPropertyMetadata(default(int))
			);

		#endregion

		#region IsInOverflow

		public bool IsInOverflow
		{
			get => CommandBar.IsCommandBarElementInOverflow(this);
			internal set => this.SetValue(IsInOverflowProperty, value);
		}

		bool ICommandBarElement3.IsInOverflow
		{
			get => IsInOverflow;
			set => IsInOverflow = value;
		}

		public static DependencyProperty IsInOverflowProperty { get; } =
			DependencyProperty.Register(
				"IsInOverflow",
				typeof(bool),
				typeof(AppBarElementContainer),
				new FrameworkPropertyMetadata(false));

		#endregion

		#region UseOverflowStyle

		internal bool UseOverflowStyle
		{
			get => (bool)this.GetValue(UseOverflowStyleProperty);
			set => this.SetValue(UseOverflowStyleProperty, value);
		}

		bool ICommandBarOverflowElement.UseOverflowStyle
		{
			get => UseOverflowStyle;
			set => UseOverflowStyle = value;
		}

		internal static DependencyProperty UseOverflowStyleProperty { get; } =
			DependencyProperty.Register(
				nameof(UseOverflowStyle),
				typeof(bool),
				typeof(AppBarElementContainer),
				new FrameworkPropertyMetadata(default(bool))
			);

		#endregion

	}
}
