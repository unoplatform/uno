using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

public sealed partial class RelativeUriRewritePage : Grid
{
	public RelativeUriRewritePage()
	{
		InitializeComponent();
	}
}

/// <summary>
/// Holds one <see cref="System.Uri"/> and one <see cref="Microsoft.UI.Xaml.Media.ImageSource"/> property, so that
/// the rewrite applied to a non-framework owner can be observed for both property types.
/// </summary>
public partial class RelativeUriHolder : Control
{
	public Uri Uri
	{
		get => (Uri)GetValue(UriProperty);
		set => SetValue(UriProperty, value);
	}

	public static DependencyProperty UriProperty { get; } =
		DependencyProperty.Register(nameof(Uri), typeof(Uri), typeof(RelativeUriHolder), new FrameworkPropertyMetadata(default(Uri)));

	public ImageSource ImageSource
	{
		get => (ImageSource)GetValue(ImageSourceProperty);
		set => SetValue(ImageSourceProperty, value);
	}

	public static DependencyProperty ImageSourceProperty { get; } =
		DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(RelativeUriHolder), new FrameworkPropertyMetadata(default(ImageSource)));
}
