#if __SKIA__
using System;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media.Media3D;

namespace Microsoft.UI.Xaml.Media;

/// <summary>
/// Represents a perspective transform (a 3-D-like effect) on an object.
/// </summary>
public partial class PlaneProjection : Projection
{
	// Perspective depth constant from WinUI PlaneProjection.h (|ZOffset| = 999)
	private const float PerspectiveDepth = 999.0f;

	/// <summary>
	/// Initializes a new instance of the PlaneProjection class.
	/// </summary>
	public PlaneProjection()
	{
	}

	#region Dependency Properties

	/// <summary>
	/// Gets or sets the x-coordinate of the center of rotation of the object you rotate.
	/// </summary>
	public double CenterOfRotationX
	{
		get => (double)GetValue(CenterOfRotationXProperty);
		set => SetValue(CenterOfRotationXProperty, value);
	}

	/// <summary>
	/// Identifies the CenterOfRotationX dependency property.
	/// </summary>
	public static DependencyProperty CenterOfRotationXProperty { get; } =
		DependencyProperty.Register(
			nameof(CenterOfRotationX),
			typeof(double),
			typeof(PlaneProjection),
			new FrameworkPropertyMetadata(0.5, OnProjectionPropertyChanged));

	/// <summary>
	/// Gets or sets the y-coordinate of the center of rotation of the object you rotate.
	/// </summary>
	public double CenterOfRotationY
	{
		get => (double)GetValue(CenterOfRotationYProperty);
		set => SetValue(CenterOfRotationYProperty, value);
	}

	/// <summary>
	/// Identifies the CenterOfRotationY dependency property.
	/// </summary>
	public static DependencyProperty CenterOfRotationYProperty { get; } =
		DependencyProperty.Register(
			nameof(CenterOfRotationY),
			typeof(double),
			typeof(PlaneProjection),
			new FrameworkPropertyMetadata(0.5, OnProjectionPropertyChanged));

	/// <summary>
	/// Gets or sets the z-coordinate of the center of rotation of the object you rotate.
	/// </summary>
	public double CenterOfRotationZ
	{
		get => (double)GetValue(CenterOfRotationZProperty);
		set => SetValue(CenterOfRotationZProperty, value);
	}

	/// <summary>
	/// Identifies the CenterOfRotationZ dependency property.
	/// </summary>
	public static DependencyProperty CenterOfRotationZProperty { get; } =
		DependencyProperty.Register(
			nameof(CenterOfRotationZ),
			typeof(double),
			typeof(PlaneProjection),
			new FrameworkPropertyMetadata(0.0, OnProjectionPropertyChanged));

	/// <summary>
	/// Gets or sets the distance that the object is rotated along the x-axis of the plane of the object.
	/// </summary>
	public double RotationX
	{
		get => (double)GetValue(RotationXProperty);
		set => SetValue(RotationXProperty, value);
	}

	/// <summary>
	/// Identifies the RotationX dependency property.
	/// </summary>
	public static DependencyProperty RotationXProperty { get; } =
		DependencyProperty.Register(
			nameof(RotationX),
			typeof(double),
			typeof(PlaneProjection),
			new FrameworkPropertyMetadata(0.0, OnProjectionPropertyChanged));

	/// <summary>
	/// Gets or sets the number of degrees to rotate the object around the y-axis of rotation.
	/// </summary>
	public double RotationY
	{
		get => (double)GetValue(RotationYProperty);
		set => SetValue(RotationYProperty, value);
	}

	/// <summary>
	/// Identifies the RotationY dependency property.
	/// </summary>
	public static DependencyProperty RotationYProperty { get; } =
		DependencyProperty.Register(
			nameof(RotationY),
			typeof(double),
			typeof(PlaneProjection),
			new FrameworkPropertyMetadata(0.0, OnProjectionPropertyChanged));

	/// <summary>
	/// Gets or sets the number of degrees to rotate the object around the z-axis of rotation.
	/// </summary>
	public double RotationZ
	{
		get => (double)GetValue(RotationZProperty);
		set => SetValue(RotationZProperty, value);
	}

	/// <summary>
	/// Identifies the RotationZ dependency property.
	/// </summary>
	public static DependencyProperty RotationZProperty { get; } =
		DependencyProperty.Register(
			nameof(RotationZ),
			typeof(double),
			typeof(PlaneProjection),
			new FrameworkPropertyMetadata(0.0, OnProjectionPropertyChanged));

	/// <summary>
	/// Gets or sets the distance the object is translated along the x-axis of the plane of the object.
	/// </summary>
	public double LocalOffsetX
	{
		get => (double)GetValue(LocalOffsetXProperty);
		set => SetValue(LocalOffsetXProperty, value);
	}

	/// <summary>
	/// Identifies the LocalOffsetX dependency property.
	/// </summary>
	public static DependencyProperty LocalOffsetXProperty { get; } =
		DependencyProperty.Register(
			nameof(LocalOffsetX),
			typeof(double),
			typeof(PlaneProjection),
			new FrameworkPropertyMetadata(0.0, OnProjectionPropertyChanged));

	/// <summary>
	/// Gets or sets the distance the object is translated along the y-axis of the plane of the object.
	/// </summary>
	public double LocalOffsetY
	{
		get => (double)GetValue(LocalOffsetYProperty);
		set => SetValue(LocalOffsetYProperty, value);
	}

	/// <summary>
	/// Identifies the LocalOffsetY dependency property.
	/// </summary>
	public static DependencyProperty LocalOffsetYProperty { get; } =
		DependencyProperty.Register(
			nameof(LocalOffsetY),
			typeof(double),
			typeof(PlaneProjection),
			new FrameworkPropertyMetadata(0.0, OnProjectionPropertyChanged));

	/// <summary>
	/// Gets or sets the distance the object is translated along the z-axis of the plane of the object.
	/// </summary>
	public double LocalOffsetZ
	{
		get => (double)GetValue(LocalOffsetZProperty);
		set => SetValue(LocalOffsetZProperty, value);
	}

	/// <summary>
	/// Identifies the LocalOffsetZ dependency property.
	/// </summary>
	public static DependencyProperty LocalOffsetZProperty { get; } =
		DependencyProperty.Register(
			nameof(LocalOffsetZ),
			typeof(double),
			typeof(PlaneProjection),
			new FrameworkPropertyMetadata(0.0, OnProjectionPropertyChanged));

	/// <summary>
	/// Gets or sets the distance the object is translated along the x-axis of the screen.
	/// </summary>
	public double GlobalOffsetX
	{
		get => (double)GetValue(GlobalOffsetXProperty);
		set => SetValue(GlobalOffsetXProperty, value);
	}

	/// <summary>
	/// Identifies the GlobalOffsetX dependency property.
	/// </summary>
	public static DependencyProperty GlobalOffsetXProperty { get; } =
		DependencyProperty.Register(
			nameof(GlobalOffsetX),
			typeof(double),
			typeof(PlaneProjection),
			new FrameworkPropertyMetadata(0.0, OnProjectionPropertyChanged));

	/// <summary>
	/// Gets or sets the distance the object is translated along the y-axis of the screen.
	/// </summary>
	public double GlobalOffsetY
	{
		get => (double)GetValue(GlobalOffsetYProperty);
		set => SetValue(GlobalOffsetYProperty, value);
	}

	/// <summary>
	/// Identifies the GlobalOffsetY dependency property.
	/// </summary>
	public static DependencyProperty GlobalOffsetYProperty { get; } =
		DependencyProperty.Register(
			nameof(GlobalOffsetY),
			typeof(double),
			typeof(PlaneProjection),
			new FrameworkPropertyMetadata(0.0, OnProjectionPropertyChanged));

	/// <summary>
	/// Gets or sets the distance the object is translated along the z-axis of the screen.
	/// </summary>
	public double GlobalOffsetZ
	{
		get => (double)GetValue(GlobalOffsetZProperty);
		set => SetValue(GlobalOffsetZProperty, value);
	}

	/// <summary>
	/// Identifies the GlobalOffsetZ dependency property.
	/// </summary>
	public static DependencyProperty GlobalOffsetZProperty { get; } =
		DependencyProperty.Register(
			nameof(GlobalOffsetZ),
			typeof(double),
			typeof(PlaneProjection),
			new FrameworkPropertyMetadata(0.0, OnProjectionPropertyChanged));

	/// <summary>
	/// Gets the projection matrix that represents this PlaneProjection.
	/// </summary>
	public Matrix3D ProjectionMatrix
	{
		get => (Matrix3D)GetValue(ProjectionMatrixProperty);
		private set => SetValue(ProjectionMatrixProperty, value);
	}

	/// <summary>
	/// Identifies the ProjectionMatrix dependency property.
	/// </summary>
	public static DependencyProperty ProjectionMatrixProperty { get; } =
		DependencyProperty.Register(
			nameof(ProjectionMatrix),
			typeof(Matrix3D),
			typeof(PlaneProjection),
			new FrameworkPropertyMetadata(Matrix3D.Identity));

	private static void OnProjectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is PlaneProjection projection)
		{
			projection.OnPropertyChanged();
		}
	}

	#endregion

	/// <summary>
	/// Calculates the projection matrix for the specified element size.
	/// </summary>
	/// <remarks>
	/// Matrix composition order (ported from WinUI PlaneProjection.cpp):
	/// Point × [LocalOffset] × [ToCenter] × [RotateX] × [RotateY] × [RotateZ] ×
	/// [FromCenter] × [GlobalOffset] × [Perspective]
	///
	/// System.Numerics uses row-vector convention: transformedPoint = point × matrix
	/// When composing: point × (A × B) = (point × A) × B, so A is applied first.
	/// </remarks>
	internal override Matrix4x4 GetProjectionMatrix(Size elementSize)
	{
		float width = (float)elementSize.Width;
		float height = (float)elementSize.Height;

		if (width == 0 || height == 0)
		{
			return Matrix4x4.Identity;
		}

		// Check if we have any 3D effect at all - if not, return identity
		if (Is2DAligned())
		{
			return Matrix4x4.Identity;
		}

		// Calculate the absolute center of rotation from the relative values
		// CenterOfRotationX/Y are relative (0.0 to 1.0), CenterOfRotationZ is absolute
		float centerX = (float)(CenterOfRotationX * width);
		float centerY = (float)(CenterOfRotationY * height);
		float centerZ = (float)CenterOfRotationZ;

		// Build the transformation matrix step by step
		// Using post-multiplication: result = result * newTransform
		// This applies transforms in the order they are added

		var result = Matrix4x4.Identity;

		// 1. Apply local offset (in object space, before rotation)
		if (LocalOffsetX != 0 || LocalOffsetY != 0 || LocalOffsetZ != 0)
		{
			result = result * Matrix4x4.CreateTranslation(
				(float)LocalOffsetX,
				(float)LocalOffsetY,
				(float)LocalOffsetZ);
		}

		// 2. Translate so that the center of rotation is at the origin
		result = result * Matrix4x4.CreateTranslation(-centerX, -centerY, -centerZ);

		// 3. Apply rotations
		// WinUI uses degrees and specific rotation direction conventions
		if (RotationX != 0)
		{
			float rotX = (float)(RotationX * Math.PI / 180.0);
			result = result * Matrix4x4.CreateRotationX(rotX);
		}
		if (RotationY != 0)
		{
			// WinUI inverts Y rotation for the visual effect
			float rotY = (float)(-RotationY * Math.PI / 180.0);
			result = result * Matrix4x4.CreateRotationY(rotY);
		}
		if (RotationZ != 0)
		{
			// WinUI inverts Z rotation
			float rotZ = (float)(-RotationZ * Math.PI / 180.0);
			result = result * Matrix4x4.CreateRotationZ(rotZ);
		}

		// 4. Translate back from center of rotation
		result = result * Matrix4x4.CreateTranslation(centerX, centerY, centerZ);

		// 5. Apply global offset (in screen space, after rotation)
		if (GlobalOffsetX != 0 || GlobalOffsetY != 0 || GlobalOffsetZ != 0)
		{
			result = result * Matrix4x4.CreateTranslation(
				(float)GlobalOffsetX,
				(float)GlobalOffsetY,
				(float)GlobalOffsetZ);
		}

		// 6. Apply perspective projection centered on the element
		// The perspective is applied at the element's center (width/2, height/2)
		// Using the WinUI perspective depth constant
		float halfWidth = width / 2;
		float halfHeight = height / 2;

		// Create the centered perspective matrix
		// This is: Translate(-halfW, -halfH) × Perspective × Translate(halfW, halfH)
		// Combined into a single matrix for efficiency
		var perspective = Matrix4x4.Identity;
		perspective.M34 = -1.0f / PerspectiveDepth;

		// Apply perspective centered on element
		result = result * Matrix4x4.CreateTranslation(-halfWidth, -halfHeight, 0);
		result = result * perspective;
		result = result * Matrix4x4.CreateTranslation(halfWidth, halfHeight, 0);

		// Update the ProjectionMatrix property
		ProjectionMatrix = Matrix3D.FromMatrix4x4(result);

		return result;
	}

	/// <summary>
	/// Checks if the projection is effectively 2D aligned (no 3D effect).
	/// </summary>
	private bool Is2DAligned()
	{
		return RotationX == 0 &&
			   RotationY == 0 &&
			   RotationZ == 0 &&
			   LocalOffsetX == 0 &&
			   LocalOffsetY == 0 &&
			   LocalOffsetZ == 0 &&
			   GlobalOffsetX == 0 &&
			   GlobalOffsetY == 0 &&
			   GlobalOffsetZ == 0;
	}
}
#endif
