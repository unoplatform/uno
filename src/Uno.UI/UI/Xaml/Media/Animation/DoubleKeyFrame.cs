using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;

namespace Windows.UI.Xaml.Media.Animation
{
	public abstract partial class DoubleKeyFrame : DependencyObject
	{
		public DoubleKeyFrame()
		{
			IsAutoPropertyInheritanceEnabled = false;
			InitializeBinder();
		}

		public DoubleKeyFrame(double value) : this()
		{
			Value = value;
		}

		public DoubleKeyFrame(double value, KeyTime keyTime) : this()
		{
			Value = value;
			KeyTime = keyTime;
		}

		/// <summary>
		/// The key frame's target value, which is the value of this key frame at its specified KeyTime. The default value is 0.
		/// </summary>
		public double Value
		{
			get { return (double)this.GetValue(ValueProperty); }
			set { this.SetValue(ValueProperty, value); }
		}
		public static DependencyProperty ValueProperty { get; } =
			DependencyProperty.Register("Value", typeof(double), typeof(DoubleKeyFrame), new FrameworkPropertyMetadata(0d));

		/// <summary>
		/// The time at which the key frame's current value should be equal to its Value property.
		/// The default value should be Uniform, but it's not supported yet so it's default(KeyTime) for now.
		/// </summary>
		public KeyTime KeyTime
		{
			get { return (KeyTime)this.GetValue(KeyTimeProperty); }
			set { this.SetValue(KeyTimeProperty, value); }
		}
		public static DependencyProperty KeyTimeProperty { get; } =
			DependencyProperty.Register("KeyTime", typeof(KeyTime), typeof(DoubleKeyFrame), new FrameworkPropertyMetadata(default(KeyTime)));


		internal abstract IEasingFunction GetEasingFunction();

		public override string ToString()
		{
			return "KeyTime: {0}, Value: {1}".InvariantCultureFormat(KeyTime, Value);
		}
	}
}
