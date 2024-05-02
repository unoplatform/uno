using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

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
		brush.GradientStops.Add(new GradientStop { Offset = 0, Color = Colors.Red });
		brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Colors.Blue });
		brush.RelativeTransform = new RotateTransform { Angle = 45 };

		Assert.IsTrue(brush.CanApplyToBorder(CornerRadius.None));
	}

	[TestMethod]
	public void When_CanApplyToBorder_Wasm_With_Radius()
	{
		var brush = new LinearGradientBrush();
		brush.GradientStops.Add(new GradientStop { Offset = 0, Color = Colors.Red });
		brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Colors.Blue });

		Assert.IsFalse(brush.CanApplyToBorder(new CornerRadius(4, 0, 0, 0));
	}
#endif

#if __IOS__ || __MACOS__
	[TestMethod]
	public void When_CanApplyToBorder_iOS_No_Transform()
	{
		var brush = new LinearGradientBrush();
		brush.GradientStops.Add(new GradientStop { Offset = 0, Color = Colors.Red });
		brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Colors.Blue });

		Assert.IsTrue(brush.CanApplyToBorder(new CornerRadius(4, 0, 0, 0));
	}

	[TestMethod]
	public void When_CanApplyToBorder_iOS_With_Transform()
	{
		var brush = new LinearGradientBrush();
		brush.GradientStops.Add(new GradientStop { Offset = 0, Color = Colors.Red });
		brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Colors.Blue });
		brush.RelativeTransform = new RotateTransform { Angle = 45 };

		Assert.IsFalse(brush.CanApplyToBorder(default);
	}
#endif


}
