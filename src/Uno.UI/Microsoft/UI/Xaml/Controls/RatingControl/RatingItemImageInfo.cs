// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RatingItemImageInfo.properties.cpp, tag winui3/release/1.5.3, commit 2a60e27c591846556fa9ec4d8f305afdf0f96dc1

using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// Represents information about the visual states of image elements that represent a rating.
/// </summary>
public partial class RatingItemImageInfo : RatingItemInfo
{
	/// <summary>
	/// Initializes a new instance of the RatingItemImageInfo class.
	/// </summary>
	public RatingItemImageInfo()
	{
	}

	/// <summary>
	/// Gets or sets an image that represents a rating element that is disabled.
	/// </summary>
	public ImageSource DisabledImage
	{
		get => (ImageSource)GetValue(DisabledImageProperty);
		set => SetValue(DisabledImageProperty, value);
	}

	/// <summary>
	/// Identifies the DisabledImage dependency property.
	/// </summary>
	public static DependencyProperty DisabledImageProperty { get; } =
		DependencyProperty.Register(
			nameof(DisabledImage),
			typeof(ImageSource),
			typeof(RatingItemImageInfo),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets an image that represents a rating element that has been set by the user.
	/// </summary>
	public ImageSource Image
	{
		get => (ImageSource)GetValue(ImageProperty);
		set => SetValue(ImageProperty, value);
	}

	/// <summary>
	/// Identifies the Image dependency property.
	/// </summary>
	public static DependencyProperty ImageProperty { get; } =
		DependencyProperty.Register(
			nameof(Image),
			typeof(ImageSource),
			typeof(RatingItemImageInfo),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets an image that represents a rating element that is showing a placeholder value.
	/// </summary>
	public ImageSource PlaceholderImage
	{
		get => (ImageSource)GetValue(PlaceholderImageProperty);
		set => SetValue(PlaceholderImageProperty, value);
	}

	/// <summary>
	/// Identifies the PlaceholderImage dependency property.
	/// </summary>
	public static DependencyProperty PlaceholderImageProperty { get; } =
		DependencyProperty.Register(
			nameof(PlaceholderImage),
			typeof(ImageSource),
			typeof(RatingItemImageInfo),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets an image that represents a rating element that has the pointer over it.
	/// </summary>
	public ImageSource PointerOverImage
	{
		get => (ImageSource)GetValue(PointerOverImageProperty);
		set => SetValue(PointerOverImageProperty, value);
	}

	/// <summary>
	/// Identifies the PointerOverImage dependency property.
	/// </summary>
	public static DependencyProperty PointerOverImageProperty { get; } =
		DependencyProperty.Register(
			nameof(PointerOverImage),
			typeof(ImageSource),
			typeof(RatingItemImageInfo),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets an image that represents a rating element showing a placeholder value with the pointer over it.
	/// </summary>
	public ImageSource PointerOverPlaceholderImage
	{
		get => (ImageSource)GetValue(PointerOverPlaceholderImageProperty);
		set => SetValue(PointerOverPlaceholderImageProperty, value);
	}

	/// <summary>
	/// Identifies the PointerOverPlaceholderImage dependency property.
	/// </summary>
	public static DependencyProperty PointerOverPlaceholderImageProperty { get; } =
		DependencyProperty.Register(
			nameof(PointerOverPlaceholderImage),
			typeof(ImageSource),
			typeof(RatingItemImageInfo),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets an image that represents a rating element that has not been set.
	/// </summary>
	public ImageSource UnsetImage
	{
		get => (ImageSource)GetValue(UnsetImageProperty);
		set => SetValue(UnsetImageProperty, value);
	}

	/// <summary>
	/// Identifies the UnsetImage dependency property.
	/// </summary>
	public static DependencyProperty UnsetImageProperty { get; } =
		DependencyProperty.Register(
			nameof(UnsetImage),
			typeof(ImageSource),
			typeof(RatingItemImageInfo),
			new FrameworkPropertyMetadata(null));
}
