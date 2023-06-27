// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Windows.Foundation;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// The OrientedSize structure is used to abstract the growth direction from
/// the layout algorithms of WrapPanel.  When the growth direction is
/// oriented horizontally (ex: the next element is arranged on the side of
/// the previous element), then the Width grows directly with the placement
/// of elements and Height grows indirectly with the size of the largest
/// element in the row.  When the orientation is reversed, so is the
/// directional growth with respect to Width and Height.
/// </summary>
/// <QualityBand>Mature</QualityBand>
internal struct OrientedSize
{
	/// <summary>
	/// The orientation of the structure.
	/// </summary>
	private Orientation _orientation;

	/// <summary>
	/// The size dimension that grows directly with layout placement.
	/// </summary>
	private double _direct;

	/// <summary>
	/// The size dimension that grows indirectly with the maximum value of
	/// the layout row or column.
	/// </summary>
	private double _indirect;

	/// <summary>
	/// Initializes a new OrientedSize structure.
	/// </summary>
	/// <param name="orientation">Orientation of the structure.</param>
	public OrientedSize(Orientation orientation) :
		this(orientation, 0.0, 0.0)
	{
	}

	/// <summary>
	/// Initializes a new OrientedSize structure.
	/// </summary>
	/// <param name="orientation">Orientation of the structure.</param>
	/// <param name="width">Un-oriented width of the structure.</param>
	/// <param name="height">Un-oriented height of the structure.</param>
	public OrientedSize(Orientation orientation, double width, double height)
	{
		_orientation = orientation;

		// All fields must be initialized before we access the this pointer
		_direct = 0.0f;
		_indirect = 0.0f;

		Width = width;
		Height = height;
	}

	/// <summary>
	/// Gets the orientation of the structure.
	/// </summary>
	public Orientation Orientation
	{
		get => _orientation;
		set => _orientation = value;
	}

	/// <summary>
	/// Gets or sets the size dimension that grows directly with layout
	/// placement.
	/// </summary>
	public double Direct
	{
		get => _direct;
		set => _direct = value;
	}

	/// <summary>
	/// Gets or sets the size dimension that grows indirectly with the
	/// maximum value of the layout row or column.
	/// </summary>
	public double Indirect
	{
		get => _indirect;
		set => _indirect = value;
	}

	/// <summary>
	/// Gets or sets the width of the size.
	/// </summary>
	public double Width
	{
		get => Orientation == Orientation.Horizontal ? Direct : Indirect;
		set
		{
			if (Orientation == Orientation.Horizontal)
			{
				Direct = value;
			}
			else
			{
				Indirect = value;
			}
		}
	}

	/// <summary>
	/// Gets or sets the height of the size.
	/// </summary>
	public double Height
	{
		get => Orientation == Orientation.Horizontal ? Indirect : Direct;
		set
		{
			if (Orientation == Orientation.Horizontal)
			{
				Indirect = value;
			}
			else
			{
				Direct = value;
			}
		}
	}

	// Helper to convert the OrientedSize into a regular Size value.
	internal Size AsUnorientedSize() => new(Width, Height);
}
