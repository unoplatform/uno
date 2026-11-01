#nullable enable

using Uno.Extensions;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Provides the base class for key frames that animate a Point property.
/// </summary>
public abstract partial class PointKeyFrame : DependencyObject, IKeyFrame
{
	protected PointKeyFrame()
	{
		IsAutoPropertyInheritanceEnabled = false;
		InitializeBinder();
	}

	/// <summary>
	/// Gets or sets the time at which the key frame's target Value should be reached.
	/// </summary>
	public KeyTime KeyTime
	{
		get => (KeyTime)GetValue(KeyTimeProperty);
		set => SetValue(KeyTimeProperty, value);
	}

	/// <summary>
	/// Identifies the KeyTime dependency property.
	/// </summary>
	public static DependencyProperty KeyTimeProperty { get; } =
		DependencyProperty.Register(nameof(KeyTime), typeof(KeyTime), typeof(PointKeyFrame), new FrameworkPropertyMetadata(default(KeyTime)));

	/// <summary>
	/// Gets or sets the key frame's target value.
	/// </summary>
	public Point Value
	{
		get => (Point)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	/// <summary>
	/// Identifies the Value dependency property.
	/// </summary>
	public static DependencyProperty ValueProperty { get; } =
		DependencyProperty.Register(nameof(Value), typeof(Point), typeof(PointKeyFrame), new FrameworkPropertyMetadata(default(Point)));

	float IKeyFrame.Percent { get; set; }

	internal virtual bool IsDiscrete => false;

	bool IKeyFrame.IsDiscrete => IsDiscrete;

	internal virtual float GetEffectiveProgress(float progress) => progress;

	float IKeyFrame.GetEffectiveProgress(float progress) => GetEffectiveProgress(progress);

	object IKeyFrame.GetValue() => Value;

	public override string ToString() => "KeyTime: {0}, Value: {1}".InvariantCultureFormat(KeyTime, Value);
}
