#if __SKIA__ || WINAPPSDK
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Media3D;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
[RunsOnUIThread]
public class Given_Matrix3DProjection
{
	[TestMethod]
	public void When_Default_Values()
	{
		var projection = new Matrix3DProjection();

		// Default ProjectionMatrix should be default Matrix3D (all zeros)
		var matrix = projection.ProjectionMatrix;
		Assert.AreEqual(0, matrix.M11);
		Assert.AreEqual(0, matrix.M22);
		Assert.AreEqual(0, matrix.M33);
		Assert.AreEqual(0, matrix.M44);
	}

	[TestMethod]
	public void When_Set_ProjectionMatrix()
	{
		var projection = new Matrix3DProjection();
		var matrix = Matrix3D.Identity;

		projection.ProjectionMatrix = matrix;

		Assert.IsTrue(projection.ProjectionMatrix.IsIdentity);
	}

	[TestMethod]
	public void When_Set_Custom_Matrix()
	{
		var projection = new Matrix3DProjection();
		var customMatrix = new Matrix3D(
			1, 0, 0, 0,
			0, 1, 0, 0,
			0, 0, 1, 0,
			10, 20, 30, 1);

		projection.ProjectionMatrix = customMatrix;

		Assert.AreEqual(10, projection.ProjectionMatrix.OffsetX);
		Assert.AreEqual(20, projection.ProjectionMatrix.OffsetY);
		Assert.AreEqual(30, projection.ProjectionMatrix.OffsetZ);
	}

	[TestMethod]
	public async Task When_Applied_To_Element()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Red)
		};

		var projection = new Matrix3DProjection
		{
			ProjectionMatrix = Matrix3D.Identity
		};

		border.Projection = projection;

		await UITestHelper.Load(border);

		Assert.AreEqual(projection, border.Projection);
		Assert.IsTrue(((Matrix3DProjection)border.Projection).ProjectionMatrix.IsIdentity);
	}

	[TestMethod]
	public async Task When_Matrix_Changed_Dynamically()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Blue)
		};

		var projection = new Matrix3DProjection
		{
			ProjectionMatrix = Matrix3D.Identity
		};
		border.Projection = projection;

		await UITestHelper.Load(border);

		// Change matrix after element is loaded
		projection.ProjectionMatrix = new Matrix3D(
			2, 0, 0, 0,
			0, 2, 0, 0,
			0, 0, 2, 0,
			0, 0, 0, 1);

		Assert.AreEqual(2, projection.ProjectionMatrix.M11);
		Assert.AreEqual(2, projection.ProjectionMatrix.M22);
		Assert.AreEqual(2, projection.ProjectionMatrix.M33);
	}

	[TestMethod]
	public async Task When_Combined_With_RenderTransform()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Green),
			RenderTransform = new RotateTransform { Angle = 45 },
			Projection = new Matrix3DProjection { ProjectionMatrix = Matrix3D.Identity }
		};

		await UITestHelper.Load(border);

		Assert.IsNotNull(border.RenderTransform);
		Assert.IsNotNull(border.Projection);
	}

#if __SKIA__
	[TestMethod]
	public void When_GetProjectionMatrix()
	{
		var customMatrix = new Matrix3D(
			1, 0, 0, 0,
			0, 1, 0, 0,
			0, 0, 1, 0,
			50, 60, 70, 1);

		var projection = new Matrix3DProjection
		{
			ProjectionMatrix = customMatrix
		};

		// GetProjectionMatrix should return the matrix converted to Matrix4x4
		var matrix4x4 = projection.GetProjectionMatrix(new Windows.Foundation.Size(100, 100));

		Assert.AreEqual(50, matrix4x4.M41, 1e-5);
		Assert.AreEqual(60, matrix4x4.M42, 1e-5);
		Assert.AreEqual(70, matrix4x4.M43, 1e-5);
	}
#endif

	[TestMethod]
	public async Task When_ProjectionMatrix_Not_Set_Element_Still_Renders()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Red)
		};

		// Assign Matrix3DProjection without setting ProjectionMatrix.
		// On WinUI the unset backing pointer causes identity to be used,
		// so the element must remain visible.
		border.Projection = new Matrix3DProjection();

		await UITestHelper.Load(border);

		var bitmap = await UITestHelper.ScreenShot(border);
		ImageAssert.HasColorAt(bitmap, bitmap.Width / 2, bitmap.Height / 2, Microsoft.UI.Colors.Red, tolerance: 25);
	}

	[TestMethod]
	public async Task When_Projection_Set_To_Null()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Yellow),
			Projection = new Matrix3DProjection { ProjectionMatrix = Matrix3D.Identity }
		};

		await UITestHelper.Load(border);

		Assert.IsNotNull(border.Projection);

		// Remove projection
		border.Projection = null;

		Assert.IsNull(border.Projection);
	}
}
#endif
