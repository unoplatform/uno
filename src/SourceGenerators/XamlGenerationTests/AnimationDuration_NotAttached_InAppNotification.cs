using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace XamlGenerationTests
{
	public partial class InAppNotification : ContentControl
	{
		/// <summary>
		/// Identifies the <see cref="AnimationDuration"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty AnimationDurationProperty =
			DependencyProperty.Register(nameof(AnimationDuration), typeof(TimeSpan), typeof(InAppNotification), new PropertyMetadata(TimeSpan.FromMilliseconds(100)));

		/// <summary>
		/// Gets or sets a value indicating the duration of the popup animation (in milliseconds).
		/// </summary>
		public TimeSpan AnimationDuration
		{
			get => (TimeSpan)this.GetValue(AnimationDurationProperty);
			set => this.SetValue(AnimationDurationProperty, value);
		}
	}
}
