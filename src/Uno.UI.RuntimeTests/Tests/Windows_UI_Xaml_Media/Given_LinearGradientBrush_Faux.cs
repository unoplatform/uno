#if __WASM__ || __IOS__ || __MACOS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
[RunsOnUIThread]
public class Given_LinearGradientBrush_Faux
{
#if __WASM__
	[TestMethod]
	public void When_CanApplyToBorder_Wasm_No_Radius()
	{
		var brush = new LinearGradientBrush();
		brush.GradientStops.Add(new GradientStop { Offset = 0, Color = Windows.UI.Colors.Red });
		brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Windows.UI.Colors.Blue });
		brush.RelativeTransform = new RotateTransform { Angle = 45 };

		Assert.IsTrue(brush.CanApplyToBorder(CornerRadius.None));
	}

	[TestMethod]
	public void When_CanApplyToBorder_Wasm_With_Radius()
	{
		var brush = new LinearGradientBrush();
		brush.GradientStops.Add(new GradientStop { Offset = 0, Color = Windows.UI.Colors.Red });
		brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Windows.UI.Colors.Blue });

		Assert.IsFalse(brush.CanApplyToBorder(new CornerRadius(4, 0, 0, 0)));
	}
#endif

#if __IOS__ || __MACOS__
	[TestMethod]
	public void When_CanApplyToBorder_iOS_No_Transform()
	{
		var brush = new LinearGradientBrush();
		brush.GradientStops.Add(new GradientStop { Offset = 0, Color = Windows.UI.Colors.Red });
		brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Windows.UI.Colors.Blue });

		Assert.IsTrue(brush.CanApplyToBorder(new CornerRadius(4, 0, 0, 0)));
	}

	[TestMethod]
	public void When_CanApplyToBorder_iOS_With_Transform()
	{
		var brush = new LinearGradientBrush();
		brush.GradientStops.Add(new GradientStop { Offset = 0, Color = Windows.UI.Colors.Red });
		brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Windows.UI.Colors.Blue });
		brush.RelativeTransform = new RotateTransform { Angle = 45 };

		Assert.IsFalse(brush.CanApplyToBorder(default));
	}
#endif

	[TestMethod]
	public void When_Dark_ControlElevationBorderBrush()
	{
		var brush = new LinearGradientBrush
		{
			FallbackColor = Windows.UI.Colors.Blue,
			MappingMode = BrushMappingMode.Absolute,
			StartPoint = new Point(0, 0),
			EndPoint = new Point(0, 3),
		};
		brush.GradientStops.Add(new GradientStop { Offset = 0.33, Color = Windows.UI.Colors.Red });
		brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Windows.UI.Colors.Blue });

		Assert.AreEqual(255, brush.FauxOverlayBrush.Color.R);
		Assert.AreEqual(VerticalAlignment.Top, brush.GetMinorStopAlignment());
	}

	[TestMethod]
	public void When_Light_ControlElevationBorderBrush()
	{
		var brush = new LinearGradientBrush
		{
			FallbackColor = Windows.UI.Colors.Blue,
			MappingMode = BrushMappingMode.Absolute,
			StartPoint = new Point(0, 0),
			EndPoint = new Point(0, 3),
			RelativeTransform = new ScaleTransform { ScaleY = -1, CenterY = 0.5 },
		};
		brush.GradientStops.Add(new GradientStop { Offset = 0.33, Color = Windows.UI.Colors.Red });
		brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Windows.UI.Colors.Blue });

		Assert.AreEqual(255, brush.FauxOverlayBrush.Color.R);
		Assert.AreEqual(VerticalAlignment.Bottom, brush.GetMinorStopAlignment());
	}

	[TestMethod]
	public void When_Dark_CircleElevationBorderBrush()
	{
		var brush = new LinearGradientBrush
		{
			FallbackColor = Windows.UI.Colors.Blue,
			MappingMode = BrushMappingMode.RelativeToBoundingBox,
			StartPoint = new Point(0, 0),
			EndPoint = new Point(0, 1),
		};
		brush.GradientStops.Add(new GradientStop { Offset = 0.7, Color = Windows.UI.Colors.Red });
		brush.GradientStops.Add(new GradientStop { Offset = 0.5, Color = Windows.UI.Colors.Blue });

		Assert.AreEqual(255, brush.FauxOverlayBrush.Color.R);
		Assert.AreEqual(VerticalAlignment.Bottom, brush.GetMinorStopAlignment());
	}

	[TestMethod]
	public void When_Light_CircleElevationBorderBrush()
	{
		var brush = new LinearGradientBrush
		{
			FallbackColor = Windows.UI.Colors.Blue,
			MappingMode = BrushMappingMode.RelativeToBoundingBox,
			StartPoint = new Point(0, 0),
			EndPoint = new Point(0, 1),
		};
		brush.GradientStops.Add(new GradientStop { Offset = 0.5, Color = Windows.UI.Colors.Blue });
		brush.GradientStops.Add(new GradientStop { Offset = 0.7, Color = Windows.UI.Colors.Red });

		Assert.AreEqual(255, brush.FauxOverlayBrush.Color.R);
		Assert.AreEqual(VerticalAlignment.Bottom, brush.GetMinorStopAlignment());
	}

	[TestMethod]
	public void When_Dark_AccentControlElevationBorderBrush()
	{
		var brush = new LinearGradientBrush
		{
			FallbackColor = Windows.UI.Colors.Blue,
			MappingMode = BrushMappingMode.Absolute,
			StartPoint = new Point(0, 0),
			EndPoint = new Point(0, 3),
			RelativeTransform = new ScaleTransform { ScaleY = -1, CenterY = 0.5 }
		};
		brush.GradientStops.Add(new GradientStop { Offset = 0.33, Color = Windows.UI.Colors.Red });
		brush.GradientStops.Add(new GradientStop { Offset = 1.0, Color = Windows.UI.Colors.Blue });

		Assert.AreEqual(255, brush.FauxOverlayBrush.Color.R);
		Assert.AreEqual(VerticalAlignment.Bottom, brush.GetMinorStopAlignment());
	}


	[TestMethod]
	public void When_Light_AccentControlElevationBorderBrush()
	{
		var brush = new LinearGradientBrush
		{
			FallbackColor = Windows.UI.Colors.Blue,
			MappingMode = BrushMappingMode.Absolute,
			StartPoint = new Point(0, 0),
			EndPoint = new Point(0, 3),
			RelativeTransform = new ScaleTransform { ScaleY = -1, CenterY = 0.5 }
		};
		brush.GradientStops.Add(new GradientStop { Offset = 0.33, Color = Windows.UI.Colors.Red });
		brush.GradientStops.Add(new GradientStop { Offset = 1.0, Color = Windows.UI.Colors.Blue });

		Assert.AreEqual(255, brush.FauxOverlayBrush.Color.R);
		Assert.AreEqual(VerticalAlignment.Bottom, brush.GetMinorStopAlignment());
	}
}
#endif
