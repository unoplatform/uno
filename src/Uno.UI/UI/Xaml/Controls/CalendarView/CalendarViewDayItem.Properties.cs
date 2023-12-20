namespace Microsoft.UI.Xaml.Controls
{

	public partial class CalendarViewDayItem //: global::Microsoft.UI.Xaml.Controls.Control
	{
		public bool IsBlackout
		{
			get
			{
				return (bool)this.GetValue(IsBlackoutProperty);
			}
			set
			{
				this.SetValue(IsBlackoutProperty, value);
			}
		}

		internal override global::System.DateTimeOffset DateBase
		{
			get => Date;
			set => Date = value;
		}
		public global::System.DateTimeOffset Date
		{
			get
			{
				return (global::System.DateTimeOffset)this.GetValue(DateProperty);
			}
			internal set
			{
				this.SetValue(DateProperty, value);
			}
		}

		public static global::Microsoft.UI.Xaml.DependencyProperty DateProperty { get; } =
			Microsoft.UI.Xaml.DependencyProperty.Register(
				nameof(Date), typeof(global::System.DateTimeOffset),
				typeof(global::Microsoft.UI.Xaml.Controls.CalendarViewDayItem),
				new FrameworkPropertyMetadata(default(global::System.DateTimeOffset)));

		public static global::Microsoft.UI.Xaml.DependencyProperty IsBlackoutProperty { get; } =
			Microsoft.UI.Xaml.DependencyProperty.Register(
				nameof(IsBlackout), typeof(bool),
				typeof(global::Microsoft.UI.Xaml.Controls.CalendarViewDayItem),
				new FrameworkPropertyMetadata(default(bool)));
	}
}
