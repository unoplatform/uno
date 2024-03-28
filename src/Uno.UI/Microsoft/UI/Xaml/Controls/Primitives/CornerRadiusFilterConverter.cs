// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference CornerRadiusFilterConverter.cpp, commit 22e5052

#nullable enable

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives;

/// <summary>
/// Converts an existing CornerRadius struct to a new CornerRadius struct, with filters applied
/// to extract only the specified fields, leaving the others set to 0.
/// </summary>
public partial class CornerRadiusFilterConverter : DependencyObject, IValueConverter
{
	/// <summary>
	/// Gets or sets the type of the filter applied to the CornerRadiusFilterConverter.
	/// </summary>
	public CornerRadiusFilterKind Filter
	{
		get => (CornerRadiusFilterKind)GetValue(FilterProperty);
		set => SetValue(FilterProperty, value);
	}

	/// <summary>
	/// Identifies the Filter dependency property.
	/// </summary>
	public static DependencyProperty FilterProperty { get; } =
		DependencyProperty.Register(
			nameof(Filter),
			typeof(CornerRadiusFilterKind),
			typeof(CornerRadiusFilterConverter),
			new FrameworkPropertyMetadata(CornerRadiusFilterKind.None));

	/// <summary>
	/// Gets or sets the scale multiplier applied to the CornerRadiusFilterConverter.
	/// </summary>
	public double Scale
	{
		get => (double)GetValue(ScaleProperty);
		set => SetValue(ScaleProperty, value);
	}

	/// <summary>
	/// Identifies the Scale dependency property.
	/// </summary>
	public static DependencyProperty ScaleProperty { get; } =
		DependencyProperty.Register(
			nameof(Scale),
			typeof(double),
			typeof(CornerRadiusFilterConverter),
			new FrameworkPropertyMetadata(1.0));

	private static CornerRadius Convert(CornerRadius radius, CornerRadiusFilterKind filterKind)
	{
		var result = radius;

		switch (filterKind)
		{
			case CornerRadiusFilterKind.Top:
				result.BottomLeft = 0;
				result.BottomRight = 0;
				break;
			case CornerRadiusFilterKind.Right:
				result.TopLeft = 0;
				result.BottomLeft = 0;
				break;
			case CornerRadiusFilterKind.Bottom:
				result.TopLeft = 0;
				result.TopRight = 0;
				break;
			case CornerRadiusFilterKind.Left:
				result.TopRight = 0;
				result.BottomRight = 0;
				break;
		}

		return result;
	}

	private static double GetDoubleValue(CornerRadius radius, CornerRadiusFilterKind filterKind) =>
		filterKind switch
		{
			CornerRadiusFilterKind.TopLeftValue => radius.TopLeft,
			CornerRadiusFilterKind.BottomRightValue => radius.BottomRight,
			_ => 0d
		};

	/// <summary>
	/// Converts the source CornerRadius by extracting only the fields specified
	/// by the Filter and leaving others set to 0.
	/// </summary>
	/// <param name="value">The source CornerRadius being passed to the target.</param>
	/// <param name="targetType">The type of the target property. Part of the IValueConverter.Convert interface method, but not used.</param>
	/// <param name="parameter">An optional parameter to be used in the converter logic. Part of the IValueConverter.Convert interface method, but not used.</param>
	/// <param name="language">The language of the conversion. Part of the IValueConverter.Convert interface method, but not used.</param>
	/// <returns>The converted CornerRadius/double value to be passed to the target dependency property.</returns>
	public object? Convert(object? value, Type targetType, object? parameter, string language)
	{
		if (value is CornerRadius cornerRadius)
		{
			var scale = Scale;
			if (!double.IsNaN(scale))
			{
				cornerRadius.TopLeft *= scale;
				cornerRadius.TopRight *= scale;
				cornerRadius.BottomLeft *= scale;
				cornerRadius.BottomRight *= scale;
			}

			var filterType = Filter;
			if (filterType == CornerRadiusFilterKind.TopLeftValue ||
				filterType == CornerRadiusFilterKind.BottomRightValue)
			{
				return GetDoubleValue(cornerRadius, filterType);
			}

			return Convert(cornerRadius, filterType);
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
