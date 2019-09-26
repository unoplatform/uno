using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class AppBarSeparator : Control, ICommandBarElement, ICommandBarElement2, ICommandBarElement3
	{
		public AppBarSeparator()
		{
			DefaultStyleKey = typeof(AppBarSeparator);
		}

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
			typeof(AppBarSeparator),
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
				typeof(AppBarSeparator),
				new FrameworkPropertyMetadata(default(int))
			);

		#endregion

		#region IsInOverflow

		public bool IsInOverflow
		{
			get => (bool)this.GetValue(IsInOverflowProperty);
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
				typeof(AppBarSeparator),
				new FrameworkPropertyMetadata(default(bool))
			);

		#endregion
	}
}
