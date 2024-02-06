using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_BorderGradientBrushHelper
{
	[TestMethod]
	public void When_No_Stop_Major_Color()
	{
		var gradient = new LinearGradientBrush();
		Assert.IsNull(BorderGradientBrushHelper.GetMajorStop(gradient));
	}

	[TestMethod]
	public void When_Dark_ControlElevationBorderBrush_Major_Color()
	{
		var gradient = new LinearGradientBrush()
		{
			MappingMode = BrushMappingMode.Absolute,
			StartPoint = new Point(0, 0),
			EndPoint = new Point(0, 3)
		};
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Black, Offset = 0.33 });
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Red, Offset = 1 });
		Assert.AreEqual(Colors.Red, BorderGradientBrushHelper.GetMajorStop(gradient).Color);
	}

	[TestMethod]
	public void When_Light_ControlElevationBorderBrush_Major_Color()
	{
		var gradient = new LinearGradientBrush()
		{
			MappingMode = BrushMappingMode.Absolute,
			StartPoint = new Point(0, 0),
			EndPoint = new Point(0, 3)
		};
		gradient.RelativeTransform = new ScaleTransform()
		{
			ScaleY = -1,
			CenterY = 0.5
		};
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Black, Offset = 0.33 });
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Red, Offset = 1 });
		Assert.AreEqual(Colors.Red, BorderGradientBrushHelper.GetMajorStop(gradient).Color);
	}

	[TestMethod]
	public void When_TextControlElevationBorderFocusedBrush_Major_Color()
	{
		var gradient = new LinearGradientBrush()
		{
			MappingMode = BrushMappingMode.Absolute,
			StartPoint = new Point(0, 0),
			EndPoint = new Point(0, 2)
		};
		gradient.RelativeTransform = new ScaleTransform()
		{
			ScaleY = -1,
			CenterY = 0.5,
		};
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Black, Offset = 1 });
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Red, Offset = 1 });
		Assert.AreEqual(Colors.Red, BorderGradientBrushHelper.GetMajorStop(gradient).Color);
	}

	[TestMethod]
	public void When_Dark_CircleElevationBorderBrush_Major_Color()
	{
		var gradient = new LinearGradientBrush()
		{
			MappingMode = BrushMappingMode.RelativeToBoundingBox,
			StartPoint = new Point(0, 0),
			EndPoint = new Point(0, 1)
		};
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Black, Offset = 0.7 }); ;
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Red, Offset = 0.5 });
		Assert.AreEqual(Colors.Red, BorderGradientBrushHelper.GetMajorStop(gradient).Color);
	}

	[TestMethod]
	public void When_Light_CircleElevationBorderBrush_Major_Color()
	{
		var gradient = new LinearGradientBrush()
		{
			MappingMode = BrushMappingMode.RelativeToBoundingBox,
			StartPoint = new Point(0, 0),
			EndPoint = new Point(0, 1)
		};
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Red, Offset = 0.5 });
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Black, Offset = 0.7 });
		Assert.AreEqual(Colors.Red, BorderGradientBrushHelper.GetMajorStop(gradient).Color);
	}

	[TestMethod]
	public void When_Dark_ControlElevationBorderBrush_Minor_Alignment()
	{
		var gradient = new LinearGradientBrush()
		{
			MappingMode = BrushMappingMode.Absolute,
			StartPoint = new Point(0, 0),
			EndPoint = new Point(0, 3)
		};
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Black, Offset = 0.33 });
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Red, Offset = 1 });
		Assert.AreEqual(VerticalAlignment.Top, BorderGradientBrushHelper.GetMinorStopAlignment(gradient));
	}

	[TestMethod]
	public void When_Light_ControlElevationBorderBrush_Minor_Alignment()
	{
		var gradient = new LinearGradientBrush()
		{
			MappingMode = BrushMappingMode.Absolute,
			StartPoint = new Point(0, 0),
			EndPoint = new Point(0, 3)
		};
		gradient.RelativeTransform = new ScaleTransform()
		{
			ScaleY = -1,
			CenterY = 0.5
		};
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Black, Offset = 0.33 });
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Red, Offset = 1 });
		Assert.AreEqual(VerticalAlignment.Bottom, BorderGradientBrushHelper.GetMinorStopAlignment(gradient));
	}

	[TestMethod]
	public void When_TextControlElevationBorderFocusedBrush_Minor_Alignment()
	{
		var gradient = new LinearGradientBrush()
		{
			MappingMode = BrushMappingMode.Absolute,
			StartPoint = new Point(0, 0),
			EndPoint = new Point(0, 2)
		};
		gradient.RelativeTransform = new ScaleTransform()
		{
			ScaleY = -1,
			CenterY = 0.5,
		};
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Black, Offset = 1 });
		gradient.GradientStops.Add(new GradientStop() { Color = Colors.Red, Offset = 1 });
		Assert.AreEqual(VerticalAlignment.Bottom, BorderGradientBrushHelper.GetMinorStopAlignment(gradient));
	}
}
