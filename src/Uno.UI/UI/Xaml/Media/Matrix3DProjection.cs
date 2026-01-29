#if __SKIA__
using System;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media.Media3D;

namespace Microsoft.UI.Xaml.Media;

/// <summary>
/// Applies a Matrix3D projection to an object.
/// </summary>
[Microsoft.UI.Xaml.Markup.ContentProperty(Name = nameof(ProjectionMatrix))]
public partial class Matrix3DProjection : Projection
{
	/// <summary>
	/// Initializes a new instance of the Matrix3DProjection class.
	/// </summary>
	public Matrix3DProjection()
	{
	}

	/// <summary>
	/// Gets or sets the Matrix3D that is used for the projection that is applied to the object.
	/// </summary>
	public Matrix3D ProjectionMatrix
	{
		get => (Matrix3D)GetValue(ProjectionMatrixProperty);
		set => SetValue(ProjectionMatrixProperty, value);
	}

	/// <summary>
	/// Identifies the ProjectionMatrix dependency property.
	/// </summary>
	public static DependencyProperty ProjectionMatrixProperty { get; } =
		DependencyProperty.Register(
			nameof(ProjectionMatrix),
			typeof(Matrix3D),
			typeof(Matrix3DProjection),
			new FrameworkPropertyMetadata(Matrix3D.Identity, OnProjectionMatrixChanged));

	private static void OnProjectionMatrixChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is Matrix3DProjection projection)
		{
			projection.OnPropertyChanged();
		}
	}

	/// <summary>
	/// Returns the projection matrix directly from the ProjectionMatrix property.
	/// </summary>
	internal override Matrix4x4 GetProjectionMatrix(Size elementSize)
	{
		return ProjectionMatrix.ToMatrix4x4();
	}
}
#endif
