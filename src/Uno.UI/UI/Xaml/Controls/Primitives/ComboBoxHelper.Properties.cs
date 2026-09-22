// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference controls/dev/ComboBox/ComboBoxHelper.idl, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ComboBoxHelper
{
	/// <summary>
	/// Identifies the KeepInteriorCornersSquare dependency property.
	/// </summary>
	public static DependencyProperty KeepInteriorCornersSquareProperty { get; } =
		DependencyProperty.RegisterAttached(
			"KeepInteriorCornersSquare",
			typeof(bool),
			typeof(ComboBoxHelper),
			new FrameworkPropertyMetadata(default(bool), OnKeepInteriorCornersSquarePropertyChanged));

	/// <summary>
	/// Sets whether the specified ComboBox keeps its interior corners square when its drop-down is open.
	/// </summary>
	/// <param name="comboBox">The ComboBox to set the property on.</param>
	/// <param name="value">The value to set.</param>
	[DynamicDependency(nameof(SetKeepInteriorCornersSquare))]
	public static void SetKeepInteriorCornersSquare(ComboBox comboBox, bool value)
		=> comboBox.SetValue(KeepInteriorCornersSquareProperty, value);

	/// <summary>
	/// Gets whether the specified ComboBox keeps its interior corners square when its drop-down is open.
	/// </summary>
	/// <param name="comboBox">The ComboBox to get the property from.</param>
	/// <returns>The value of the KeepInteriorCornersSquare attached property.</returns>
	[DynamicDependency(nameof(GetKeepInteriorCornersSquare))]
	public static bool GetKeepInteriorCornersSquare(ComboBox comboBox)
		=> (bool)comboBox.GetValue(KeepInteriorCornersSquareProperty);
}
