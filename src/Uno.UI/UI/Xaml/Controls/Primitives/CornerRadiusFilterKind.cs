// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference CornerRadiusFilterConverters.idl, commit c6174f1

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Defines constants that specify the filter type for a CornerRadiusFilterConverter instance.
/// </summary>
public enum CornerRadiusFilterKind
{
	/// <summary>
	/// No filter applied.
	/// </summary>
	None,

	/// <summary>
	/// Filters TopLeft and TopRight values, sets BottomLeft and BottomRight to 0.
	/// </summary>
	Top,

	/// <summary>
	/// Filters TopRight and BottomRight values, sets TopLeft and BottomLeft to 0.
	/// </summary>
	Right,

	/// <summary>
	/// Filters BottomLeft and BottomRight values, sets TopLeft and TopRight to 0.
	/// </summary>
	Bottom,

	/// <summary>
	/// Filters TopLeft and BottomLeft values, sets TopRight and BottomRight to 0.
	/// </summary>
	Left,

	/// <summary>
	/// Gets the double value of TopLeft corner.
	/// </summary>
	TopLeftValue,

	/// <summary>
	/// Gets the double value of BottomRight corner.
	/// </summary>
	BottomRightValue
}
