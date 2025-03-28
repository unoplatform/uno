using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI;
using Windows.UI.Composition;
using Windows.UI.Composition.Interactions;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public partial class Given_ExpressionAnimation
{
	[TestMethod]
	public async Task When_Animating_Visual_Offset()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Windows.UI.Colors.Red),
		};

		var visual = ElementCompositionPreview.GetElementVisual(border);
		var myVisual = ElementCompositionPreview.GetElementVisual(new Border());

		await UITestHelper.Load(border);

		var expressionAnimation = visual.Compositor.CreateExpressionAnimation("myVisual.Offset");
		expressionAnimation.SetReferenceParameter("myVisual", myVisual);

		visual.StartAnimation("Offset", expressionAnimation);
		try
		{
			Assert.AreEqual(Vector3.Zero, visual.Offset);
			Assert.AreEqual(Vector3.Zero, myVisual.Offset);

			myVisual.Offset = new Vector3(10, 20, 0);
			Assert.AreEqual(new Vector3(10, 20, 0), visual.Offset);
			Assert.AreEqual(new Vector3(10, 20, 0), myVisual.Offset);
		}
		finally
		{
			visual.StopAnimation("Offset");
		}
	}

	[TestMethod]
	public async Task When_Animating_Visual_Translation()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Windows.UI.Colors.Red),
		};

		var visual = ElementCompositionPreview.GetElementVisual(border);
		var myVisual = ElementCompositionPreview.GetElementVisual(new Border());

		await UITestHelper.Load(border);

		var expressionAnimation = visual.Compositor.CreateExpressionAnimation("myVisual.Offset");
		expressionAnimation.SetReferenceParameter("myVisual", myVisual);

		visual.Properties.InsertVector3("Translation", Vector3.Zero);

		visual.StartAnimation("Translation", expressionAnimation);
		try
		{
			Assert.AreEqual(Vector3.Zero, visual.Offset);
			Assert.AreEqual(Vector3.Zero, myVisual.Offset);

			myVisual.Offset = new Vector3(10, 20, 0);
			Assert.AreEqual(CompositionGetValueStatus.Succeeded, visual.Properties.TryGetVector3("Translation", out var visualTranslation));
			Assert.AreEqual(new Vector3(10, 20, 0), visualTranslation);
			Assert.AreEqual(new Vector3(10, 20, 0), myVisual.Offset);
		}
		finally
		{
			visual.StopAnimation("Translation");
		}
	}

	[TestMethod]
	public async Task When_Animating_Visual_Offset_Using_Vector3_Call()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Windows.UI.Colors.Red),
		};

		var visual = ElementCompositionPreview.GetElementVisual(border);
		var myVisual = ElementCompositionPreview.GetElementVisual(new Border());

		await UITestHelper.Load(border);

		var expressionAnimation = visual.Compositor.CreateExpressionAnimation("Vector3(myVisual.Offset.X, myVisual.Offset.Y, myVisual.Offset.Z)");
		expressionAnimation.SetReferenceParameter("myVisual", myVisual);

		visual.StartAnimation("Offset", expressionAnimation);
		try
		{
			Assert.AreEqual(Vector3.Zero, visual.Offset);
			Assert.AreEqual(Vector3.Zero, myVisual.Offset);

			myVisual.Offset = new Vector3(10, 20, 0);
			Assert.AreEqual(new Vector3(10, 20, 0), visual.Offset);
			Assert.AreEqual(new Vector3(10, 20, 0), myVisual.Offset);
		}
		finally
		{
			visual.StopAnimation("Offset");
		}
	}

	[TestMethod]
	public async Task When_Animating_Visual_Offset_X()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Windows.UI.Colors.Red),
		};

		var visual = ElementCompositionPreview.GetElementVisual(border);
		var myVisual = ElementCompositionPreview.GetElementVisual(new Border());

		await UITestHelper.Load(border);

		var expressionAnimation = visual.Compositor.CreateExpressionAnimation("myVisual.Offset.X");
		expressionAnimation.SetReferenceParameter("myVisual", myVisual);

		visual.StartAnimation("Offset.X", expressionAnimation);
		try
		{
			Assert.AreEqual(Vector3.Zero, visual.Offset);
			Assert.AreEqual(Vector3.Zero, myVisual.Offset);

			myVisual.Offset = new Vector3(10, 20, 0);
			Assert.AreEqual(new Vector3(10, 0, 0), visual.Offset);
			Assert.AreEqual(new Vector3(10, 20, 0), myVisual.Offset);
		}
		finally
		{
			visual.StopAnimation("Offset.X");
		}
	}

	[TestMethod]
	public async Task When_Animating_Visual_Translation_X()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Windows.UI.Colors.Red),
		};

		var visual = ElementCompositionPreview.GetElementVisual(border);
		var myVisual = ElementCompositionPreview.GetElementVisual(new Border());

		await UITestHelper.Load(border);

		var expressionAnimation = visual.Compositor.CreateExpressionAnimation("myVisual.Offset.X");
		expressionAnimation.SetReferenceParameter("myVisual", myVisual);

		visual.Properties.InsertVector3("Translation", Vector3.Zero);

		visual.StartAnimation("Translation.X", expressionAnimation);
		try
		{
			Assert.AreEqual(Vector3.Zero, visual.Offset);
			Assert.AreEqual(Vector3.Zero, myVisual.Offset);

			myVisual.Offset = new Vector3(10, 20, 0);
			Assert.AreEqual(CompositionGetValueStatus.Succeeded, visual.Properties.TryGetVector3("Translation", out var visualTranslation));
			Assert.AreEqual(new Vector3(10, 0, 0), visualTranslation);
			Assert.AreEqual(new Vector3(10, 20, 0), myVisual.Offset);
		}
		finally
		{
			visual.StopAnimation("Translation.X");
		}
	}

	[TestMethod]
	[UnoWorkItem("https://github.com/unoplatform/uno/issues/16570")]
#if !__SKIA__
	[Ignore("Only supported on Skia")]
#endif
	public async Task When_Animating_CompositionPropertySet()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Windows.UI.Colors.Red),
		};

		var visual = ElementCompositionPreview.GetElementVisual(border);
		ElementCompositionPreview.SetIsTranslationEnabled(border, true);

		await UITestHelper.Load(border);

		var cps = visual.Compositor.CreatePropertySet();

		var expressionAnimation = visual.Compositor.CreateExpressionAnimation("cps.X");
		expressionAnimation.SetReferenceParameter("cps", cps);

		cps.InsertScalar("X", 75);

		Assert.AreEqual(CompositionGetValueStatus.NotFound, visual.Properties.TryGetVector3("Translation", out _));
		Assert.AreEqual(CompositionGetValueStatus.NotFound, cps.TryGetVector3("Translation", out _));

		visual.StartAnimation("Translation.X", expressionAnimation);
		try
		{
			Assert.AreEqual(CompositionGetValueStatus.Succeeded, visual.Properties.TryGetVector3("Translation", out var visualTranslation));
			Assert.AreEqual(CompositionGetValueStatus.Succeeded, visual.Properties.TryGetVector3("Translation", out var cpsTranslation));
			Assert.AreEqual(new Vector3(75, 0, 0), visualTranslation);
			Assert.AreEqual(new Vector3(75, 0, 0), cpsTranslation);
		}
		finally
		{
			visual.StopAnimation("Translation.X");
		}
	}
}
