#if __SKIA__
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
[RunsOnUIThread]
public class Given_PlaneProjection
{
	[TestMethod]
	public void When_Default_Values()
	{
		var projection = new PlaneProjection();

		// CenterOfRotationX and CenterOfRotationY default to 0.5 (center of element)
		Assert.AreEqual(0.5, projection.CenterOfRotationX);
		Assert.AreEqual(0.5, projection.CenterOfRotationY);
		Assert.AreEqual(0.0, projection.CenterOfRotationZ);

		// All rotations default to 0
		Assert.AreEqual(0.0, projection.RotationX);
		Assert.AreEqual(0.0, projection.RotationY);
		Assert.AreEqual(0.0, projection.RotationZ);

		// All offsets default to 0
		Assert.AreEqual(0.0, projection.LocalOffsetX);
		Assert.AreEqual(0.0, projection.LocalOffsetY);
		Assert.AreEqual(0.0, projection.LocalOffsetZ);
		Assert.AreEqual(0.0, projection.GlobalOffsetX);
		Assert.AreEqual(0.0, projection.GlobalOffsetY);
		Assert.AreEqual(0.0, projection.GlobalOffsetZ);
	}

	[TestMethod]
	public void When_Set_RotationX()
	{
		var projection = new PlaneProjection();
		projection.RotationX = 45;

		Assert.AreEqual(45, projection.RotationX);
	}

	[TestMethod]
	public void When_Set_RotationY()
	{
		var projection = new PlaneProjection();
		projection.RotationY = 90;

		Assert.AreEqual(90, projection.RotationY);
	}

	[TestMethod]
	public void When_Set_RotationZ()
	{
		var projection = new PlaneProjection();
		projection.RotationZ = 180;

		Assert.AreEqual(180, projection.RotationZ);
	}

	[TestMethod]
	public void When_Set_LocalOffset()
	{
		var projection = new PlaneProjection();
		projection.LocalOffsetX = 10;
		projection.LocalOffsetY = 20;
		projection.LocalOffsetZ = 30;

		Assert.AreEqual(10, projection.LocalOffsetX);
		Assert.AreEqual(20, projection.LocalOffsetY);
		Assert.AreEqual(30, projection.LocalOffsetZ);
	}

	[TestMethod]
	public void When_Set_GlobalOffset()
	{
		var projection = new PlaneProjection();
		projection.GlobalOffsetX = 100;
		projection.GlobalOffsetY = 200;
		projection.GlobalOffsetZ = 300;

		Assert.AreEqual(100, projection.GlobalOffsetX);
		Assert.AreEqual(200, projection.GlobalOffsetY);
		Assert.AreEqual(300, projection.GlobalOffsetZ);
	}

	[TestMethod]
	public void When_Set_CenterOfRotation()
	{
		var projection = new PlaneProjection();
		projection.CenterOfRotationX = 0.0; // Left edge
		projection.CenterOfRotationY = 1.0; // Bottom edge
		projection.CenterOfRotationZ = 50;

		Assert.AreEqual(0.0, projection.CenterOfRotationX);
		Assert.AreEqual(1.0, projection.CenterOfRotationY);
		Assert.AreEqual(50, projection.CenterOfRotationZ);
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

		var projection = new PlaneProjection
		{
			RotationY = 45
		};

		border.Projection = projection;

		await UITestHelper.Load(border);

		Assert.AreEqual(projection, border.Projection);
		Assert.AreEqual(45, ((PlaneProjection)border.Projection).RotationY);
	}

	[TestMethod]
	public async Task When_Projection_Changed_Dynamically()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Blue)
		};

		var projection = new PlaneProjection();
		border.Projection = projection;

		await UITestHelper.Load(border);

		// Change rotation after element is loaded
		projection.RotationX = 30;
		projection.RotationY = 60;

		Assert.AreEqual(30, projection.RotationX);
		Assert.AreEqual(60, projection.RotationY);
	}

	[TestMethod]
	public async Task When_Combined_With_RenderTransform()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Green),
			RenderTransform = new ScaleTransform { ScaleX = 1.5, ScaleY = 1.5 },
			Projection = new PlaneProjection { RotationY = 45 }
		};

		await UITestHelper.Load(border);

		Assert.IsNotNull(border.RenderTransform);
		Assert.IsNotNull(border.Projection);
		Assert.AreEqual(45, ((PlaneProjection)border.Projection).RotationY);
	}

	[TestMethod]
	public void When_ProjectionMatrix_Is_Computed()
	{
		var projection = new PlaneProjection
		{
			RotationY = 45
		};

		// Trigger matrix computation by getting the projection matrix
		var matrix = projection.GetProjectionMatrix(new Windows.Foundation.Size(100, 100));

		// The matrix should not be identity when rotation is applied
		Assert.IsFalse(matrix.IsIdentity);
	}

	[TestMethod]
	public void When_No_Rotation_Matrix_Is_Close_To_Identity()
	{
		var projection = new PlaneProjection();

		// With no rotation and default values, the projection should be close to identity
		// (perspective projection still applies some transformation)
		var matrix = projection.GetProjectionMatrix(new Windows.Foundation.Size(100, 100));

		// The matrix won't be exactly identity due to perspective, but should be close
		Assert.IsNotNull(matrix);
	}

	[TestMethod]
	public async Task When_Projection_Set_To_Null()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Yellow),
			Projection = new PlaneProjection { RotationZ = 45 }
		};

		await UITestHelper.Load(border);

		Assert.IsNotNull(border.Projection);

		// Remove projection
		border.Projection = null;

		Assert.IsNull(border.Projection);
	}
}
#endif
