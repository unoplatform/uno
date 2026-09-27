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
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public partial class Given_ExpressionAnimation
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Animating_Visual_Translation()
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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Animating_Visual_Offset_X()
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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Animating_Visual_Translation_X()
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
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/16570")]
#if !__SKIA__
	[Ignore("Only supported on Skia")]
#endif
	public async Task When_Animating_CompositionPropertySet()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
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

	// LottieGen reconfigures a single ExpressionAnimation (different Expression + reference parameters)
	// and starts it on many targets. Each target must keep the parameters it was started with, not the
	// last-configured ones. (This is why the "L" in the LottieFiles logo used to disappear.)
	[TestMethod]
#if !__SKIA__
	[Ignore("ExpressionAnimation evaluation is Skia-only")]
#endif
	public async Task When_Reusable_Expression_Started_On_Multiple_Targets()
	{
		var border = new Border() { Width = 100, Height = 100 };
		await UITestHelper.Load(border);

		var compositor = ElementCompositionPreview.GetElementVisual(border).Compositor;

		var sourceA = compositor.CreatePropertySet();
		sourceA.InsertScalar("x", 1f);
		var sourceB = compositor.CreatePropertySet();
		sourceB.InsertScalar("x", 2f);

		var targetA = compositor.CreatePropertySet();
		targetA.InsertScalar("y", 0f);
		var targetB = compositor.CreatePropertySet();
		targetB.InsertScalar("y", 0f);

		var reusable = compositor.CreateExpressionAnimation();

		reusable.ClearAllParameters();
		reusable.Expression = "_.x";
		reusable.SetReferenceParameter("_", sourceA);
		targetA.StartAnimation("y", reusable);

		// Same instance, reconfigured for a different source.
		reusable.ClearAllParameters();
		reusable.Expression = "_.x";
		reusable.SetReferenceParameter("_", sourceB);
		targetB.StartAnimation("y", reusable);

		targetA.TryGetScalar("y", out var a0);
		targetB.TryGetScalar("y", out var b0);
		Assert.AreEqual(1f, a0, 0.001f); // targetA tracks sourceA
		Assert.AreEqual(2f, b0, 0.001f); // targetB tracks sourceB

		sourceA.InsertScalar("x", 10f);
		sourceB.InsertScalar("x", 20f);

		targetA.TryGetScalar("y", out var a1);
		targetB.TryGetScalar("y", out var b1);
		Assert.AreEqual(10f, a1, 0.001f);
		Assert.AreEqual(20f, b1, 0.001f);
	}
}
