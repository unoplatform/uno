#nullable enable

using System;
using Windows.System;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class RepeatButton : ButtonBase
	{
		/// <summary>
		/// Initializes a new instance of the RepeatButton class.
		/// </summary>
		public RepeatButton()
		{
			DefaultStyleKey = typeof(RepeatButton);
		}

		/// <summary>
		/// Gets or sets the time, in milliseconds, that the RepeatButton waits when it is pressed before it starts repeating the click action.
		/// </summary>
		public int Delay
		{
			get => (int)GetValue(DelayProperty);
			set => SetValue(DelayProperty, value);
		}

		/// <summary>
		/// Identifies the Delay dependency property.
		/// </summary>
		public static DependencyProperty DelayProperty { get; } =
			DependencyProperty.Register(
				nameof(Delay),
				typeof(int),
				typeof(RepeatButton),
				new FrameworkPropertyMetadata(250));

		/// <summary>
		/// Gets or sets the time, in milliseconds, between repetitions of the click action, as soon as repeating starts.
		/// </summary>
		public int Interval
		{
			get => (int)GetValue(IntervalProperty);
			set => SetValue(IntervalProperty, value);
		}

		/// <summary>
		/// Identifies the Interval dependency property.
		/// </summary>
		public static DependencyProperty IntervalProperty { get; } =
			DependencyProperty.Register(
				nameof(Interval),
				typeof(int),
				typeof(RepeatButton),
				new FrameworkPropertyMetadata(250));
	}
}
