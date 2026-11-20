#if HAS_UNO
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

[TestClass]
[RunsOnUIThread]
public class Given_ThemeGeneratorHelper
{
	private static TimeSpan Ms(long ms) => TimeSpan.FromTicks(ms * 10000);

	[TestMethod]
	public void When_Easing_Null_KeyFrame_Is_Discrete()
	{
		var (border, sb, helper) = CreateHelper();

		helper.RegisterKeyFrame(helper.GetOpacityPropertyName(), 0.5, 0, 100, null);

		var timeline = SingleDouble(sb);
		var keyFrame = timeline.KeyFrames.Single();
		Assert.IsInstanceOfType(keyFrame, typeof(DiscreteDoubleKeyFrame));
		Assert.AreEqual(0.5, keyFrame.Value);
		Assert.AreEqual(Ms(100), keyFrame.KeyTime.TimeSpan);
		Assert.AreSame(border.TransitionTarget, timeline.Target);
		Assert.AreEqual("Opacity", Storyboard.GetTargetProperty(timeline));
		Assert.IsTrue(timeline.IsThemeGenerated);
	}

	[TestMethod]
	public void When_Easing_Linear_KeyFrame_Is_Linear()
	{
		var (_, sb, helper) = CreateHelper();

		helper.RegisterKeyFrame(helper.GetOpacityPropertyName(), 0.5, 0, 100, new TimingFunctionDescription());

		Assert.IsInstanceOfType(SingleDouble(sb).KeyFrames.Single(), typeof(LinearDoubleKeyFrame));
	}

	[TestMethod]
	public void When_Easing_Curve_KeyFrame_Is_Spline_From_Inner_Control_Points()
	{
		var (border, sb, helper) = CreateHelper();

		helper.RegisterKeyFrame(helper.GetScaleXPropertyName(), 2, 0, 100, Curve());

		var timeline = SingleDouble(sb);
		var keyFrame = (SplineDoubleKeyFrame)timeline.KeyFrames.Single();
		Assert.AreEqual(new Point(0.1f, 0.9f), keyFrame.KeySpline.ControlPoint1);
		Assert.AreEqual(new Point(0.2f, 1f), keyFrame.KeySpline.ControlPoint2);
		Assert.AreSame(border.TransitionTarget!.CompositeTransform, timeline.Target);
		Assert.AreEqual("ScaleX", Storyboard.GetTargetProperty(timeline));
	}

	[TestMethod]
	public void When_Steady_State_Keeps_Only_Last_KeyFrame_At_Zero()
	{
		var (_, sb, helper) = CreateHelper(onlyGenerateSteadyState: true);

		helper.RegisterKeyFrame(helper.GetTranslateXPropertyName(), 10, 50, 0, Curve());
		helper.RegisterKeyFrame(helper.GetTranslateXPropertyName(), 20, 50, 200, Curve());

		var timeline = SingleDouble(sb);
		var keyFrame = timeline.KeyFrames.Single();
		Assert.IsInstanceOfType(keyFrame, typeof(LinearDoubleKeyFrame));
		Assert.AreEqual(20, keyFrame.Value);
		Assert.AreEqual(TimeSpan.Zero, keyFrame.KeyTime.TimeSpan);
		Assert.AreEqual(TimeSpan.Zero, timeline.BeginTime, "Steady state never offsets the timeline");
	}

	[TestMethod]
	public void When_First_KeyFrame_Has_Begin_Time_Timeline_Starts_Then()
	{
		var (_, sb, helper) = CreateHelper();

		helper.RegisterKeyFrame(helper.GetTranslateXPropertyName(), 10, 100, 0, Curve());
		helper.RegisterKeyFrame(helper.GetTranslateXPropertyName(), 20, 100, 200, Curve());

		var timeline = SingleDouble(sb);
		Assert.AreEqual(Ms(100), timeline.BeginTime);
		CollectionAssert.AreEqual(
			new[] { TimeSpan.Zero, Ms(200) },
			timeline.KeyFrames.Select(k => k.KeyTime.TimeSpan).ToArray());
	}

	[TestMethod]
	public void When_Same_Property_Registered_Twice_One_Timeline()
	{
		var (_, sb, helper) = CreateHelper();

		helper.RegisterKeyFrame(helper.GetClipTranslateXPropertyName(), 0, 0, 0, Curve());
		helper.RegisterKeyFrame(helper.GetClipTranslateXPropertyName(), 5, 0, 100, Curve());
		helper.RegisterKeyFrame(helper.GetClipTranslateYPropertyName(), 5, 0, 100, Curve());

		Assert.AreEqual(2, sb.Children.Count);
		var clipX = (DoubleAnimationUsingKeyFrames)sb.Children[0];
		Assert.AreEqual(2, clipX.KeyFrames.Count);
		Assert.AreEqual("TranslateX", Storyboard.GetTargetProperty(clipX));
		Assert.AreEqual("TranslateY", Storyboard.GetTargetProperty(sb.Children[1]));
	}

	[TestMethod]
	public void When_FadeIn()
	{
		var (border, sb) = Generate(ThemeGenerator.TAS_FADEIN, ThemeGenerator.TA_FADEIN_SHOWN);

		AssertAllThemeGenerated(sb);
		var opacity = SingleDouble(sb);
		Assert.AreSame(border.TransitionTarget, opacity.Target);
		Assert.AreEqual("Opacity", Storyboard.GetTargetProperty(opacity));
		AssertKeyFrame<LinearDoubleKeyFrame>(opacity.KeyFrames.Single(), 1.0, 167);
	}

	[TestMethod]
	public void When_FadeOut()
	{
		var (_, sb) = Generate(ThemeGenerator.TAS_FADEOUT, ThemeGenerator.TA_FADEOUT_HIDDEN);

		AssertAllThemeGenerated(sb);
		AssertKeyFrame<LinearDoubleKeyFrame>(SingleDouble(sb).KeyFrames.Single(), 0.0, 167);
	}

	[TestMethod]
	public void When_DragSourceStart_DragSource()
	{
		var (border, sb) = Generate(ThemeGenerator.TAS_DRAGSOURCESTART, ThemeGenerator.TA_DRAGSOURCESTART_DRAGSOURCE);
		var tt = border.TransitionTarget!;

		AssertAllThemeGenerated(sb);
		Assert.AreEqual(4, sb.Children.Count);

		AssertOrigin(sb.Children[0], tt, "TransformOrigin");
		AssertScale(sb, tt, (double)1.05f);

		var opacity = (DoubleAnimationUsingKeyFrames)sb.Children[3];
		Assert.AreSame(tt, opacity.Target);
		Assert.AreEqual("Opacity", Storyboard.GetTargetProperty(opacity));
		AssertCurveKeyFrame(opacity.KeyFrames.Single(), (double)0.65f, 240);
	}

	[TestMethod]
	public void When_DragSourceStart_Affected()
	{
		var (border, sb) = Generate(ThemeGenerator.TAS_DRAGSOURCESTART, ThemeGenerator.TA_DRAGSOURCESTART_AFFECTED);
		var tt = border.TransitionTarget!;

		AssertAllThemeGenerated(sb);
		Assert.AreEqual(3, sb.Children.Count);
		AssertOrigin(sb.Children[0], tt, "TransformOrigin");
		AssertScale(sb, tt, (double)0.95f);
	}

	[TestMethod]
	[DataRow(ThemeGenerator.TAS_DRAGBETWEENENTER, ThemeGenerator.TA_DRAGBETWEENENTER_AFFECTED)]
	[DataRow(ThemeGenerator.TAS_DRAGBETWEENLEAVE, ThemeGenerator.TA_DRAGBETWEENLEAVE_AFFECTED)]
	public void When_DragBetween_Translates_To_Destination(int storyboardID, int targetID)
	{
		var (border, sb) = Generate(storyboardID, targetID, startOffset: new Point(1, 2), destinationOffset: new Point(30, 40));
		var tt = border.TransitionTarget!;

		AssertAllThemeGenerated(sb);
		Assert.AreEqual(2, sb.Children.Count);

		var x = (DoubleAnimationUsingKeyFrames)sb.Children[0];
		var y = (DoubleAnimationUsingKeyFrames)sb.Children[1];
		Assert.AreSame(tt.CompositeTransform, x.Target);
		Assert.AreEqual("TranslateX", Storyboard.GetTargetProperty(x));
		Assert.AreEqual("TranslateY", Storyboard.GetTargetProperty(y));

		AssertCurveKeyFrame(x.KeyFrames[0], 1, 0);
		AssertCurveKeyFrame(x.KeyFrames[1], 30, 200);
		AssertCurveKeyFrame(y.KeyFrames[0], 2, 0);
		AssertCurveKeyFrame(y.KeyFrames[1], 40, 200);
	}

	[TestMethod]
	public void When_Steady_State_PVL_Keeps_End_Value_At_Zero()
	{
		var (_, sb) = Generate(ThemeGenerator.TAS_DRAGBETWEENENTER, ThemeGenerator.TA_DRAGBETWEENENTER_AFFECTED, onlyGenerateSteadyState: true, destinationOffset: new Point(30, 40));

		var x = (DoubleAnimationUsingKeyFrames)sb.Children[0];
		AssertKeyFrame<LinearDoubleKeyFrame>(x.KeyFrames.Single(), 30, 0);
	}

	[TestMethod]
	public void When_Origin_Set_Twice_Only_First_Is_Kept()
	{
		var (border, sb, helper) = CreateHelper();

		helper.Set2DTransformOriginValues(new Point(0.5, 0.5));
		helper.Set2DTransformOriginValues(new Point(1, 1));
		helper.SetClipOriginValues(new Point(0.25, 0.75));

		Assert.AreEqual(2, sb.Children.Count);
		AssertOrigin(sb.Children[0], border.TransitionTarget!, "TransformOrigin");
		AssertOrigin(sb.Children[1], border.TransitionTarget!, "ClipTransformOrigin", new Point(0.25, 0.75));
	}

	[TestMethod]
	public async Task When_Origin_Storyboard_Begins_Value_Applies_And_Reverts_On_Stop()
	{
		var (border, sb, helper) = CreateHelper();
		await UITestHelper.Load(border);

		helper.Set2DTransformOriginValues(new Point(0.5, 0.5));
		helper.SetClipOriginValues(new Point(0.25, 0.75));
		var tt = border.TransitionTarget!;

		sb.Begin();

		Assert.AreEqual(new Point(0.5, 0.5), tt.TransformOrigin, "Applied synchronously during Begin");
		Assert.AreEqual(new Point(0.25, 0.75), tt.ClipTransformOrigin);

		await Task.Delay(100);
		Assert.AreEqual(new Point(0.5, 0.5), tt.TransformOrigin, "Held after the zero duration");

		sb.Stop();

		Assert.AreEqual(default(Point), tt.TransformOrigin);
		Assert.AreEqual(default(Point), tt.ClipTransformOrigin);
	}

	[TestMethod]
	public async Task When_DragSourceStart_Runs_To_End_And_Reverts_On_Stop()
	{
		Border border = new() { Width = 50, Height = 50 };
		await UITestHelper.Load(border);

		var (_, sb) = Generate(ThemeGenerator.TAS_DRAGSOURCESTART, ThemeGenerator.TA_DRAGSOURCESTART_DRAGSOURCE, border: border);
		var tt = border.TransitionTarget!;

		sb.Begin();
		await Task.Delay(600);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1.05, tt.CompositeTransform!.ScaleX, 0.001);
		Assert.AreEqual(1.05, tt.CompositeTransform.ScaleY, 0.001);
		Assert.AreEqual(0.65, tt.Opacity, 0.001);
		Assert.AreEqual(new Point(0.5, 0.5), tt.TransformOrigin);

		sb.Stop();

		Assert.AreEqual(1.0, tt.CompositeTransform.ScaleX);
		Assert.AreEqual(1.0, tt.Opacity);
		Assert.AreEqual(default(Point), tt.TransformOrigin);
	}

	private static TimingFunctionDescription Curve() => new()
	{
		cp2 = new Point(0.1f, 0.9f),
		cp3 = new Point(0.2f, 1f),
	};

	private static (Border border, Storyboard sb, ThemeGeneratorHelper helper) CreateHelper(bool onlyGenerateSteadyState = false)
	{
		Border border = new() { Width = 50, Height = 50 };
		Storyboard sb = new();
		ThemeGeneratorHelper helper = new(default, default, null, border, onlyGenerateSteadyState, sb.Children);
		helper.Initialize();
		return (border, sb, helper);
	}

	private static (Border border, Storyboard sb) Generate(
		int storyboardID,
		int targetID,
		bool onlyGenerateSteadyState = false,
		Point startOffset = default,
		Point destinationOffset = default,
		Border border = null)
	{
		border ??= new() { Width = 50, Height = 50 };
		Storyboard sb = new();
		ThemeGenerator.AddTimelinesForThemeAnimation(storyboardID, targetID, null, border, onlyGenerateSteadyState, startOffset, destinationOffset, sb.Children);
		return (border, sb);
	}

	private static DoubleAnimationUsingKeyFrames SingleDouble(Storyboard sb) => (DoubleAnimationUsingKeyFrames)sb.Children.Single();

	private static void AssertAllThemeGenerated(Storyboard sb)
	{
		Assert.IsTrue(sb.Children.Count > 0);
		foreach (var timeline in sb.Children)
		{
			Assert.IsTrue(timeline.IsThemeGenerated, $"{timeline.GetType().Name} {Storyboard.GetTargetProperty(timeline)}");
		}
	}

	private static void AssertKeyFrame<T>(DoubleKeyFrame keyFrame, double value, long keyTimeMs)
	{
		Assert.IsInstanceOfType(keyFrame, typeof(T));
		Assert.AreEqual(value, keyFrame.Value);
		Assert.AreEqual(Ms(keyTimeMs), keyFrame.KeyTime.TimeSpan);
	}

	private static void AssertCurveKeyFrame(DoubleKeyFrame keyFrame, double value, long keyTimeMs)
	{
		AssertKeyFrame<SplineDoubleKeyFrame>(keyFrame, value, keyTimeMs);
		var spline = ((SplineDoubleKeyFrame)keyFrame).KeySpline;
		Assert.AreEqual(new Point(0.1f, 0.9f), spline.ControlPoint1);
		Assert.AreEqual(new Point(0.2f, 1f), spline.ControlPoint2);
	}

	private static void AssertScale(Storyboard sb, TransitionTarget tt, double scale)
	{
		var scaleX = (DoubleAnimationUsingKeyFrames)sb.Children[1];
		var scaleY = (DoubleAnimationUsingKeyFrames)sb.Children[2];
		Assert.AreSame(tt.CompositeTransform, scaleX.Target);
		Assert.AreSame(tt.CompositeTransform, scaleY.Target);
		Assert.AreEqual("ScaleX", Storyboard.GetTargetProperty(scaleX));
		Assert.AreEqual("ScaleY", Storyboard.GetTargetProperty(scaleY));
		AssertCurveKeyFrame(scaleX.KeyFrames.Single(), scale, 240);
		AssertCurveKeyFrame(scaleY.KeyFrames.Single(), scale, 240);
	}

	private static void AssertOrigin(Timeline timeline, TransitionTarget tt, string property, Point? expected = null)
	{
		var origin = (ObjectAnimationUsingKeyFrames)timeline;
		Assert.IsTrue(origin.IsThemeGenerated);
		Assert.AreSame(tt, origin.Target);
		Assert.AreEqual(property, Storyboard.GetTargetProperty(origin));
		Assert.AreEqual(new Duration(TimeSpan.Zero), origin.Duration);

		var keyFrame = origin.KeyFrames.Single();
		Assert.IsInstanceOfType(keyFrame, typeof(DiscreteObjectKeyFrame));
		Assert.AreEqual(TimeSpan.Zero, keyFrame.KeyTime.TimeSpan);
		Assert.AreEqual(expected ?? new Point(0.5, 0.5), keyFrame.Value);
	}
}
#endif
