// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference CornerRadiusFilterConverters.idl, commit c6174f1

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Defines constants that specify the filter type for a CornerRadiusToThicknessConverter instance.
/// </summary>
public enum CornerRadiusToThicknessConverterKind
{
	/// <summary>
	/// Filters Top and Bottom values from TopLeft and BottomLeft values.
	/// </summary>
	FilterTopAndBottomFromLeft,

	/// <summary>
	/// Filters Top and Bottom values from TopRight and BottomRight values.
	/// </summary>
	FilterTopAndBottomFromRight,

	/// <summary>
	/// Filters Left and Right values from TopLeft and TopRight values.
	/// </summary>
	FilterLeftAndRightFromTop,

	/// <summary>
	/// Filters Left and Right values from BottomLeft and BottomRight values.
	/// </summary>
	FilterLeftAndRightFromBottom,

	/// <summary>
	/// Filters Top value from TopLeft value.
	/// </summary>
	FilterTopFromTopLeft,

	/// <summary>
	/// Filters Top value from TopRight value.
	/// </summary>
	FilterTopFromTopRight,

	/// <summary>
	/// Filters Right value from TopRight value.
	/// </summary>
	FilterRightFromTopRight,

	/// <summary>
	/// Filters Right value from BottomRight value.
	/// </summary>
	FilterRightFromBottomRight,

	/// <summary>
	/// Filters Bottom value from BottomRight value.
	/// </summary>
	FilterBottomFromBottomRight,

	/// <summary>
	/// Filters Bottom value from BottomLeft value.
	/// </summary>
	FilterBottomFromBottomLeft,

	/// <summary>
	/// Filters Left value from BottomLeft value.
	/// </summary>
	FilterLeftFromBottomLeft,

	/// <summary>
	/// Filters Left value from TopLeft value.
	/// </summary>
	FilterLeftFromTopLeft,
}
