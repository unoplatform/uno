// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference CornerRadiusToThicknessConverter.cpp, commit 8aaf7f8

#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Converts a CornerRadius to Thickness and also applies filters to extract only the specified fields, leaving the others set to 0.
/// </summary>
public sealed partial class CornerRadiusToThicknessConverter : DependencyObject, IValueConverter
{
	/// <summary>
	/// Gets or sets the conversion kind that will be applied to the CornerRadiusToThicknessConverter.
	/// </summary>
	public CornerRadiusToThicknessConverterKind ConversionKind
	{
		get => (CornerRadiusToThicknessConverterKind)GetValue(ConversionKindProperty);
		set => SetValue(ConversionKindProperty, value);
	}

	/// <summary>
	/// Identifies the ConversionKind dependency property.
	/// </summary>
	public static DependencyProperty ConversionKindProperty { get; } =
		DependencyProperty.Register(
			nameof(ConversionKind),
			typeof(CornerRadiusToThicknessConverterKind),
			typeof(CornerRadiusToThicknessConverter),
			new FrameworkPropertyMetadata(CornerRadiusToThicknessConverterKind.FilterLeftAndRightFromTop));

	/// <summary>
	/// Gets or sets the value of the multiplier for the radius.
	/// </summary>
	public double Multiplier
	{
		get => (double)GetValue(MultiplierProperty);
		set => SetValue(MultiplierProperty, value);
	}

	/// <summary>
	/// Identifies the Multiplier dependency property.
	/// </summary>
	public static DependencyProperty MultiplierProperty { get; } =
		DependencyProperty.Register(
			nameof(Multiplier),
			typeof(double),
			typeof(CornerRadiusToThicknessConverter),
			new FrameworkPropertyMetadata(1.0));

	private static Thickness Convert(CornerRadius radius, CornerRadiusToThicknessConverterKind filterKind, double multiplier)
	{
		var result = new Thickness { };

		switch (filterKind)
		{
			case CornerRadiusToThicknessConverterKind.FilterLeftAndRightFromTop:
				result.Left = radius.TopLeft * multiplier;
				result.Right = radius.TopRight * multiplier;
				result.Top = 0;
				result.Bottom = 0;
				break;
			case CornerRadiusToThicknessConverterKind.FilterLeftAndRightFromBottom:
				result.Left = radius.BottomLeft * multiplier;
				result.Right = radius.BottomRight * multiplier;
				result.Top = 0;
				result.Bottom = 0;
				break;
			case CornerRadiusToThicknessConverterKind.FilterTopAndBottomFromLeft:
				result.Left = 0;
				result.Right = 0;
				result.Top = radius.TopLeft * multiplier;
				result.Bottom = radius.BottomLeft * multiplier;
				break;
			case CornerRadiusToThicknessConverterKind.FilterTopAndBottomFromRight:
				result.Left = 0;
				result.Right = 0;
				result.Top = radius.TopRight * multiplier;
				result.Bottom = radius.BottomRight * multiplier;
				break;
			case CornerRadiusToThicknessConverterKind.FilterTopFromTopLeft:
				result.Left = 0;
				result.Right = 0;
				result.Top = radius.TopLeft * multiplier;
				result.Bottom = 0;
				break;
			case CornerRadiusToThicknessConverterKind.FilterTopFromTopRight:
				result.Left = 0;
				result.Right = 0;
				result.Top = radius.TopRight * multiplier;
				result.Bottom = 0;
				break;
			case CornerRadiusToThicknessConverterKind.FilterRightFromTopRight:
				result.Left = 0;
				result.Right = radius.TopRight * multiplier;
				result.Top = 0;
				result.Bottom = 0;
				break;
			case CornerRadiusToThicknessConverterKind.FilterRightFromBottomRight:
				result.Left = 0;
				result.Right = radius.BottomRight * multiplier;
				result.Top = 0;
				result.Bottom = 0;
				break;
			case CornerRadiusToThicknessConverterKind.FilterBottomFromBottomRight:
				result.Left = 0;
				result.Right = 0;
				result.Top = 0;
				result.Bottom = radius.BottomRight * multiplier;
				break;
			case CornerRadiusToThicknessConverterKind.FilterBottomFromBottomLeft:
				result.Left = 0;
				result.Right = 0;
				result.Top = 0;
				result.Bottom = radius.BottomLeft * multiplier;
				break;
			case CornerRadiusToThicknessConverterKind.FilterLeftFromBottomLeft:
				result.Left = radius.BottomLeft * multiplier;
				result.Right = 0;
				result.Top = 0;
				result.Bottom = 0;
				break;
			case CornerRadiusToThicknessConverterKind.FilterLeftFromTopLeft:
				result.Left = radius.TopLeft * multiplier;
				result.Right = 0;
				result.Top = 0;
				result.Bottom = 0;
				break;
		}

		return result;
	}

	/// <summary>
	/// Converts a CornerRadius value to a Thickness value, while also extracting the fields specified by ConversionKind (leaving others set to 0).
	/// </summary>
	/// <param name="value">The source CornerRadius being passed to the target.</param>
	/// <param name="targetType">The type of the target property. Part of the IValueConverter.Convert interface method, but not used.</param>
	/// <param name="parameter">An optional parameter to be used in the converter logic. Part of the IValueConverter.Convert interface method, but not used.</param>
	/// <param name="language">The language of the conversion. Part of the IValueConverter.Convert interface method, but not used.</param>
	/// <returns>The converted Thickness value to be passed to the target dependency property.</returns>
	public object? Convert(object? value, Type targetType, object? parameter, string language)
	{
		if (value is CornerRadius radius)
		{
			var multiplier = Multiplier;
			return Convert(radius, ConversionKind, multiplier);
		}
		return null;
	}

	/// <summary>
	/// Not implemented.
	/// </summary>
	/// <exception cref="NotImplementedException">Always thrown when called.</exception>
	public object? ConvertBack(object? value, Type targetType, object? parameter, string language) =>
		throw new NotImplementedException();
}
