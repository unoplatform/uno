using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Media3D;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Media.Projection;

[Sample("Projection", Name = "Matrix3DProjection_Basic", Description = "Matrix3DProjection with custom 4x4 transformation matrix")]
public sealed partial class Matrix3DProjection_Basic : Page
{
	public Matrix3DProjection_Basic()
	{
		this.InitializeComponent();
	}

	private void OnApplyIdentity(object sender, RoutedEventArgs e)
	{
		IdentityRect.Projection = new Matrix3DProjection
		{
			ProjectionMatrix = Matrix3D.Identity
		};
	}

	private void OnApplyTranslation(object sender, RoutedEventArgs e)
	{
		var translateX = TranslateXSlider.Value;
		var translateY = TranslateYSlider.Value;
		var translateZ = TranslateZSlider.Value;

		// Create a translation matrix
		var matrix = new Matrix3D(
			1, 0, 0, 0,
			0, 1, 0, 0,
			0, 0, 1, 0,
			translateX, translateY, translateZ, 1);

		TranslationRect.Projection = new Matrix3DProjection
		{
			ProjectionMatrix = matrix
		};
	}

	private void OnApplyScale(object sender, RoutedEventArgs e)
	{
		var scaleX = ScaleXSlider.Value;
		var scaleY = ScaleYSlider.Value;

		// Create a scale matrix
		var matrix = new Matrix3D(
			scaleX, 0, 0, 0,
			0, scaleY, 0, 0,
			0, 0, 1, 0,
			0, 0, 0, 1);

		ScaleRect.Projection = new Matrix3DProjection
		{
			ProjectionMatrix = matrix
		};
	}

	private void OnApplyPerspective(object sender, RoutedEventArgs e)
	{
		var perspective = PerspectiveSlider.Value;
		var rotationY = PerspectiveRotationY.Value * Math.PI / 180.0;

		// Create rotation Y matrix
		var cosY = Math.Cos(rotationY);
		var sinY = Math.Sin(rotationY);

		// Combine rotation with perspective
		var matrix = new Matrix3D(
			cosY, 0, sinY, 0,
			0, 1, 0, 0,
			-sinY, 0, cosY, perspective,
			0, 0, 0, 1);

		PerspectiveRect.Projection = new Matrix3DProjection
		{
			ProjectionMatrix = matrix
		};
	}
}
