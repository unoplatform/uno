#nullable enable

using System.Numerics;
using Windows.Foundation;

namespace Windows.UI.Composition;

public partial class CompositionViewBox : CompositionObject
{
	private float _verticalAlignmentRatio;
	private CompositionStretch _stretch;
	private Vector2 _size;
	private Vector2 _offset;
	private float _horizontalAlignmentRatio;

	internal CompositionViewBox(Compositor compositor) : base(compositor)
	{
	}

	public float VerticalAlignmentRatio
	{
		get => _verticalAlignmentRatio;
		set => SetProperty(ref _verticalAlignmentRatio, value);
	}

	public CompositionStretch Stretch
	{
		get => _stretch;
		set => SetEnumProperty(ref _stretch, value);
	}

	public Vector2 Size
	{
		get => _size;
		set => SetProperty(ref _size, value);
	}

	public Vector2 Offset
	{
		get => _offset;
		set => SetProperty(ref _offset, value);
	}

	public float HorizontalAlignmentRatio
	{
		get => _horizontalAlignmentRatio;
		set => SetProperty(ref _horizontalAlignmentRatio, value);
	}

	internal Rect GetRect()
		=> new(
			x: Offset.X,
			y: Offset.Y,
			width: Size.X,
			height: Size.Y);
}
