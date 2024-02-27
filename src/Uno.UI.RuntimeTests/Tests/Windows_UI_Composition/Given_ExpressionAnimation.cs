using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
internal partial class Given_ExpressionAnimation
{
	[TestMethod]
	public async Task When_Animating_Visual_Offset()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
		};

		var visual = ElementCompositionPreview.GetElementVisual(border);
		var myVisual = ElementCompositionPreview.GetElementVisual(new Border());

		await UITestHelper.Load(border);

		var expressionAnimation = visual.Compositor.CreateExpressionAnimation("myVisual.Offset");
		expressionAnimation.SetReferenceParameter("myVisual", myVisual);

		visual.StartAnimation("Offset", expressionAnimation);
		try
		{
			Assert.AreEqual(visual.Offset, Vector3.Zero);
			Assert.AreEqual(myVisual.Offset, Vector3.Zero);

			myVisual.Offset = new Vector3(10, 20, 0);
			Assert.AreEqual(visual.Offset, new Vector3(10, 20, 0));
			Assert.AreEqual(myVisual.Offset, new Vector3(10, 20, 0));
		}
		finally
		{
			visual.StopAnimation("Offset");
		}
	}

	[TestMethod]
	public async Task When_Animating_Visual_Offset_Using_Vector3_Call()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
		};

		var visual = ElementCompositionPreview.GetElementVisual(border);
		var myVisual = ElementCompositionPreview.GetElementVisual(new Border());

		await UITestHelper.Load(border);

		var expressionAnimation = visual.Compositor.CreateExpressionAnimation("Vector3(myVisual.Offset.X, myVisual.Offset.Y, myVisual.Offset.Z)");
		expressionAnimation.SetReferenceParameter("myVisual", myVisual);

		visual.StartAnimation("Offset", expressionAnimation);
		try
		{
			Assert.AreEqual(visual.Offset, Vector3.Zero);
			Assert.AreEqual(myVisual.Offset, Vector3.Zero);

			myVisual.Offset = new Vector3(10, 20, 0);
			Assert.AreEqual(visual.Offset, new Vector3(10, 20, 0));
			Assert.AreEqual(myVisual.Offset, new Vector3(10, 20, 0));
		}
		finally
		{
			visual.StopAnimation("Offset");
		}
	}
}
