using System;
using Uno.Extensions;
using Windows.Foundation;

namespace Windows.UI.Xaml;

/// <summary>
/// Represents the options that can be applied when an element is brought into view.
/// </summary>
public partial class BringIntoViewOptions
{
	private double _horizontalAlignmentRatio = double.NaN;
	private double _verticalAlignmentRatio = double.NaN;
	private double _verticalOffset = 0;
	private double _horizontalOffset = 0;

	/// <summary>
	/// Initializes a new instance of the BringIntoViewOptions class.
	/// </summary>
	public BringIntoViewOptions()
	{
	}

	/// <summary>
	/// Gets or sets a value that indicates whether to use animation when the element is brought into view.
	/// </summary>
	public bool AnimationDesired { get; set; } = false;

	/// <summary>
	/// Controls the positioning of the vertical axis of the TargetRect with respect
	/// to the vertical axis of the viewport. The value is clamped from 0.0f to 1.0f
	/// with 0.0f representing the left vertical edge and 1.0f representing the right
	/// vertical edge. By default this is set to 0.0f.
	/// </summary>
	public double HorizontalAlignmentRatio
	{
		get => _horizontalAlignmentRatio;
		set
		{
			if (!double.IsNaN(value))
			{
				value = value.Clamp(0.0, 1.0);
			}
			_horizontalAlignmentRatio = value;
		}
	}

	/// <summary>
	/// Gets or sets the horizontal distance to add to the viewport-relative
	/// position of the TargetRect after satisfying the requested HorizontalAlignmentRatio.
	/// </summary>
	public double HorizontalOffset
	{
		get => _horizontalOffset;
		set
		{
			if (double.IsNaN(value) || double.IsInfinity(value))
			{
				throw new ArgumentOutOfRangeException(nameof(value));
			}
			_horizontalOffset = value;
		}
	}

	/// <summary>
	/// Gets or sets the area of an element to bring into view.
	/// </summary>
	public Rect? TargetRect { get; set; } = null;

	/// <summary>
	/// Controls the positioning of the horizontal axis of the TargetRect with respect
	/// to the horizontal axis of the viewport. The value is clamped from 0.0f to 1.0f
	/// with 0.0f representing the top horizontal edge and 1.0f representing the bottom
	/// horizontal edge. By default this is set to 0.0f.
	/// </summary>
	public double VerticalAlignmentRatio
	{
		get => _verticalAlignmentRatio;
		set
		{
			if (!double.IsNaN(value))
			{
				value = value.Clamp(0.0, 1.0);
			}
			_verticalAlignmentRatio = value;
		}
	}
	/// <summary>
	/// Gets or sets the vertical distance to add to the viewport-relative
	/// position of the TargetRect after satisfying the requested VerticalAlignmentRatio.
	/// </summary>
	public double VerticalOffset
	{
		get => _verticalOffset;
		set
		{
			if (double.IsNaN(value) || double.IsInfinity(value))
			{
				throw new ArgumentOutOfRangeException(nameof(value));
			}
			_verticalOffset = value;
		}
	}
}
