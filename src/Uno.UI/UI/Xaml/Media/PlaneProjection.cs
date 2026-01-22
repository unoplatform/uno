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
	// Constants from WinUI PlaneProjection.h
	private const bool RightHanded = true;
	private const float NearPlane = 1.0f;
	private const float FarPlane = 1001.0f;
	private const float FieldOfView = 57.0f; // degrees
	private const float ZOffset = -999.0f;   // for right-handed coordinate system

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
	/// [Centering] × [LocalOffset] × [RotateCenter] × [RotateX] × [RotateY] × [RotateZ] ×
	/// [UndoRotateCenter] × [GlobalOffset] × [ZOffset] × [Perspective] × [UndoCentering]
	/// </remarks>
	internal override Matrix4x4 GetProjectionMatrix(Size elementSize)
	{
		float width = (float)elementSize.Width;
		float height = (float)elementSize.Height;

		// Calculate the absolute center of rotation from the relative values
		// CenterOfRotationX/Y are relative (0.0 to 1.0), CenterOfRotationZ is absolute
		float centerX = (float)(CenterOfRotationX * width);
		float centerY = (float)(CenterOfRotationY * height);
		float centerZ = (float)CenterOfRotationZ;

		// Convert degrees to radians
		float rotX = (float)(RotationX * Math.PI / 180.0);
		float rotY = (float)(RotationY * Math.PI / 180.0);
		float rotZ = (float)(RotationZ * Math.PI / 180.0);

		// For right-handed coordinate system, invert Y rotation
		if (RightHanded)
		{
			rotY = -rotY;
		}

		// 1. Move element center to origin
		var centering = Matrix4x4.CreateTranslation(-width / 2, -height / 2, 0);

		// 2. Apply local offset (in object space, before rotation)
		var localOffset = Matrix4x4.CreateTranslation(
			(float)LocalOffsetX,
			(float)-LocalOffsetY, // Y is inverted in the visual coordinate system
			(float)LocalOffsetZ);

		// 3. Move to rotation center
		var rotateCenter = Matrix4x4.CreateTranslation(-centerX + width / 2, -centerY + height / 2, -centerZ);

		// 4. Apply rotations (X, then Y, then Z)
		var rotationX = Matrix4x4.CreateRotationX(rotX);
		var rotationY = Matrix4x4.CreateRotationY(rotY);
		var rotationZ = Matrix4x4.CreateRotationZ(rotZ);

		// 5. Undo rotation center translation
		var undoRotateCenter = Matrix4x4.CreateTranslation(centerX - width / 2, centerY - height / 2, centerZ);

		// 6. Apply global offset (in screen space, after rotation)
		var globalOffset = Matrix4x4.CreateTranslation(
			(float)GlobalOffsetX,
			(float)-GlobalOffsetY, // Y is inverted in the visual coordinate system
			(float)GlobalOffsetZ);

		// 7. Apply Z offset for perspective (moves away from camera)
		var zOffset = Matrix4x4.CreateTranslation(0, 0, ZOffset);

		// 8. Apply perspective projection
		// Based on WinUI's field of view calculation
		float fovRadians = FieldOfView * (float)Math.PI / 180.0f;
		float cotFov = 1.0f / (float)Math.Tan(fovRadians / 2.0f);

		var perspective = new Matrix4x4(
			cotFov, 0, 0, 0,
			0, cotFov, 0, 0,
			0, 0, FarPlane / (FarPlane - NearPlane), 1,
			0, 0, -NearPlane * FarPlane / (FarPlane - NearPlane), 0);

		// 9. Undo Z offset and centering
		var undoZOffset = Matrix4x4.CreateTranslation(0, 0, -ZOffset);
		var undoCentering = Matrix4x4.CreateTranslation(width / 2, height / 2, 0);

		// Compose all matrices
		var result = centering * localOffset * rotateCenter *
					 rotationX * rotationY * rotationZ *
					 undoRotateCenter * globalOffset *
					 zOffset * perspective * undoZOffset * undoCentering;

		// Update the ProjectionMatrix property
		ProjectionMatrix = Matrix3D.FromMatrix4x4(result);

		return result;
	}
}
#endif
