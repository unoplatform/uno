// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ObjectRun.h, ObjectRun.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// ObjectRun is a subclass of TextRun that indicates an embedded UIElement and
/// exposes APIs to access element metrics.
/// </summary>
internal abstract class ObjectRun : TextRun
{
	private readonly TextRunProperties m_pProperties;

	// Initializes a new instance of ObjectRun class.
	protected ObjectRun(uint characterIndex, TextRunProperties pProperties)
		: base(2, characterIndex, TextRunType.Object)
	{
		m_pProperties = pProperties;
	}

	// Flag indicates whether text object has fixed size regardless of where it is
	// placed within a line.
	public abstract bool HasFixedSize();

	// Gets text object measurement metrics that will fit within the specified
	// remaining width of the paragraph.
	public abstract Result Format(
		TextSource pTextSource,
		float paragraphWidth,
		Point currentPosition,
		out ObjectRunMetrics pMetrics);

	// Computes bounding box of text object.
	public abstract Result ComputeBoundingBox(
		bool rightToLeft,
		bool sideways,
		out Rect pBounds);

	// Arranges the embedded object within its host container.
	public abstract Result Arrange(Point position);

	// Returns text run properties associated with this object.
	public TextRunProperties GetProperties() => m_pProperties;
}
