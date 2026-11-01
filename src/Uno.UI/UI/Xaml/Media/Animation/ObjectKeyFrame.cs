using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class ObjectKeyFrame : DependencyObject, IKeyFrame
	{
		protected ObjectKeyFrame()
		{
			IsAutoPropertyInheritanceEnabled = false;
			InitializeBinder();
		}

		#region KeyTime Dependency Property
		public KeyTime KeyTime
		{
			get => (KeyTime)this.GetValue(KeyTimeProperty);
			set => this.SetValue(KeyTimeProperty, value);
		}

		public static DependencyProperty KeyTimeProperty { get; } =
			DependencyProperty.Register("KeyTime", typeof(KeyTime), typeof(ObjectKeyFrame), new FrameworkPropertyMetadata(default(KeyTime)));
		#endregion

		#region Value Dependency Property
		public object Value
		{
			get => (object)this.GetValue(ValueProperty);
			set => this.SetValue(ValueProperty, value);
		}

		public static DependencyProperty ValueProperty { get; } =
			DependencyProperty.Register("Value", typeof(object), typeof(ObjectKeyFrame), new FrameworkPropertyMetadata(null));
		#endregion

		float IKeyFrame.Percent { get; set; }

		// Object values can't be interpolated: every object key frame is discrete.
		internal virtual bool IsDiscrete => true;

		bool IKeyFrame.IsDiscrete => IsDiscrete;

		internal virtual float GetEffectiveProgress(float progress) => 0;

		float IKeyFrame.GetEffectiveProgress(float progress) => GetEffectiveProgress(progress);

		object IKeyFrame.GetValue() => Value;
	}
}
