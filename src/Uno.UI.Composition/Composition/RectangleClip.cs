#nullable enable

using System.Numerics;

namespace Windows.UI.Composition;

/// <summary>
/// Represents a rectangle with optional rounded corners that clips a portion of a visual.
/// The portion of the visual inside the rectangle is visible; the portion of the visual outside the rectangle is clipped.
/// </summary>
public partial class RectangleClip : CompositionClip
{
	private float _left;
	private float _top;
	private float _right;
	private float _bottom;
	private Vector2 _topLeftRadius;
	private Vector2 _topRightRadius;
	private Vector2 _bottomRightRadius;
	private Vector2 _bottomLeftRadius;

	internal RectangleClip(Compositor compositor) : base(compositor)
	{
	}

	/// <summary>
	/// Gets or sets the offset from the left of the visual. The portion of the visual
	/// to the left of the edge defined by Left will be clipped. Animatable.
	/// </summary>
	public float Left
	{
		get => _left;
		set => SetProperty(ref _left, value);
	}

	/// <summary>
	/// Gets or sets the offset from the top of the visual. The portion of the visual above
	/// the edge defined by Top will be clipped. Animatable.
	/// </summary>
	public float Top
	{
		get => _top;
		set => SetProperty(ref _top, value);
	}

	/// <summary>
	/// Gets or sets the offset from the right of the visual. The portion of the visual
	/// to the right the edge defined by Right will be clipped. Animatable.
	/// </summary>
	public float Right
	{
		get => _right;
		set => SetProperty(ref _right, value);
	}

	/// <summary>
	/// Gets or sets the offset from the bottom of the visual. The portion of the visual
	/// below the edge defined by Bottom will be clipped. Animatable.
	/// </summary>
	public float Bottom
	{
		get => _bottom;
		set => SetProperty(ref _bottom, value);
	}

	/// <summary>
	/// Gets or sets the amount by which the top left corner of the rectangle is rounded.
	/// </summary>
	public Vector2 TopLeftRadius
	{
		get => _topLeftRadius;
		set => SetProperty(ref _topLeftRadius, value);
	}

	/// <summary>
	/// Gets or sets the amount by which the top right corner of the rectangle is rounded.
	/// </summary>
	public Vector2 TopRightRadius
	{
		get => _topRightRadius;
		set => SetProperty(ref _topRightRadius, value);
	}

	/// <summary>
	/// Gets or sets the amount by which the bottom right corner of the rectangle is rounded.
	/// </summary>
	public Vector2 BottomRightRadius
	{
		get => _bottomRightRadius;
		set => SetProperty(ref _bottomRightRadius, value);
	}

	/// <summary>
	/// Gets or sets the amount by which the top right corner of the rectangle is rounded.
	/// </summary>
	public Vector2 BottomLeftRadius
	{
		get => _bottomLeftRadius;
		set => SetProperty(ref _bottomLeftRadius, value);
	}
}
