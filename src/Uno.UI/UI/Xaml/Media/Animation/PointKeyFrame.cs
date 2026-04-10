using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public abstract partial class PointKeyFrame : DependencyObject
	{
		public PointKeyFrame()
		{
			IsAutoPropertyInheritanceEnabled = false;
			InitializeBinder();
		}

		public PointKeyFrame(Point value) : this()
		{
			Value = value;
		}

		public PointKeyFrame(Point value, KeyTime keyTime) : this()
		{
			Value = value;
			KeyTime = keyTime;
		}

		/// <summary>
		/// The key frame's target value, which is the value of this key frame at its specified KeyTime. The default value is (0,0).
		/// </summary>
		public Point Value
		{
			get { return (Point)this.GetValue(ValueProperty); }
			set { this.SetValue(ValueProperty, value); }
		}
		public static DependencyProperty ValueProperty { get; } =
			DependencyProperty.Register("Value", typeof(Point), typeof(PointKeyFrame), new FrameworkPropertyMetadata(default(Point)));

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
			DependencyProperty.Register("KeyTime", typeof(KeyTime), typeof(PointKeyFrame), new FrameworkPropertyMetadata(default(KeyTime)));


		internal abstract IEasingFunction GetEasingFunction();

		public override string ToString()
		{
			return "KeyTime: {0}, Value: {1}".InvariantCultureFormat(KeyTime, Value);
		}
	}
}
