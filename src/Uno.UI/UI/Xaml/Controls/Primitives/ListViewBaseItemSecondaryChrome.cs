// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemSecondaryChrome.h, ListViewBaseItemSecondaryChrome.cpp, tag winui3/release/1.4.2

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Secondary chrome for ListView/GridView items that owns and renders the earmark geometry.
/// </summary>
internal partial class ListViewBaseItemSecondaryChrome : FrameworkElement
{
	// Earmark geometry data - Chrome2 owns the earmark.
	private Geometry? _earmarkGeometryData;

	// A pointer to the primary chrome.
	internal ListViewBaseItemChrome? PrimaryChromeNoRef { get; set; }

	// Earmark geometry bounds
	private Rect _earmarkGeometryBounds;

	// Earmark rendering fields.
	private bool _fillBrushDirty;

	public ListViewBaseItemSecondaryChrome()
	{
	}

	/// <summary>
	/// Gets the bounds of the earmark geometry.
	/// </summary>
	internal Rect GetEarmarkBounds() => _earmarkGeometryBounds;

	/// <summary>
	/// Builds the earmark path geometry (a triangle in the top-right corner).
	/// </summary>
	internal void PrepareEarmarkPath()
	{
		if (_earmarkGeometryData != null)
		{
			return;
		}

		// The earmark is a right triangle: (0,0) -> (40,0) -> (40,40) -> back to start
		var startPoint = new Point(0.0f, 0.0f);
		var point1 = new Point(40.0f, 0.0f);
		var point2 = new Point(40.0f, 40.0f);

		var figure = new PathFigure
		{
			StartPoint = startPoint,
			IsClosed = true,
			IsFilled = true,
		};

		figure.Segments.Add(new LineSegment { Point = point1 });
		figure.Segments.Add(new LineSegment { Point = point2 });

		var geometry = new PathGeometry();
		geometry.Figures.Add(figure);

		// Store the bounds
		_earmarkGeometryBounds = geometry.Bounds;
		_earmarkGeometryData = geometry;
	}

	internal void NWSetContentDirty()
	{
		_fillBrushDirty = true;
		InvalidateArrange();
	}

	internal void NWCleanDirtyFlags()
	{
		_fillBrushDirty = false;
	}

	internal bool IsFillBrushDirty => _fillBrushDirty;
	internal Geometry? EarmarkGeometry => _earmarkGeometryData;
}
