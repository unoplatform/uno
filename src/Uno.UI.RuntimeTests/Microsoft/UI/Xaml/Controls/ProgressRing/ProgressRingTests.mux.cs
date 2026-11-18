// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ProgressRing/APITests/ProgressRingTests.cs and ProgressRing/InteractionTests/ProgressRingTests.cs

using System;
using System.Threading.Tasks;
using Combinatorial.MSTest;
using Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.Disposables;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls.ProgressRingTests;

[TestClass]
public class ProgressRingTests
{
	[TestMethod]
	public void VerifyAccessibilityView()
	{
		RunOnUIThread.Execute(() =>
		{
			var progressRing = new ProgressRing();
			progressRing.IsActive = true;

			Verify.AreNotEqual(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(progressRing));

			progressRing.IsActive = false;
			Verify.AreEqual(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(progressRing));
		});
	}

	// Interaction tests: the UIA queries against the test page are replaced by direct reads of the
	// same template parts and automation peer the page reported through its TextBlocks.

	[TestMethod]
	[RunsOnUIThread]
	public async Task ChangeStateTest()
	{
		var progressRing = new ProgressRing();
		await UITestHelper.Load(progressRing);

		await VerifyStateTransitions(progressRing);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task LottieCustomSourceTest()
	{
		var indeterminateSource = new global::Microsoft.UI.Xaml.Controls.AnimatedVisuals.ProgressRingIndeterminate();
		var determinateSource = new global::Microsoft.UI.Xaml.Controls.AnimatedVisuals.ProgressRingDeterminate();
		var progressRing = new ProgressRing
		{
			IndeterminateSource = indeterminateSource,
			DeterminateSource = determinateSource,
		};
		await UITestHelper.Load(progressRing);

		var (_, player) = GetTemplateParts(progressRing);
		Verify.AreEqual(indeterminateSource, player.Source);

		await VerifyStateTransitions(progressRing, () => Verify.AreEqual(determinateSource, player.Source));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task StoryboardAnimationRetemplateTest()
	{
		var progressRing = new ProgressRing
		{
			Height = 32,
			Width = 32,
			Style = (Style)XamlReader.Load(
				"""
				<Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:controls="using:Microsoft.UI.Xaml.Controls"
					TargetType="controls:ProgressRing">
					<Setter Property="Template">
						<Setter.Value>
							<ControlTemplate TargetType="controls:ProgressRing">
								<Grid x:Name="Ring"
									Background="{TemplateBinding Background}"
									BorderThickness="{TemplateBinding BorderThickness}"
									BorderBrush="{TemplateBinding BorderBrush}"
									Padding="{TemplateBinding Padding}"
									MaxWidth="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.MaxSideLength}"
									MaxHeight="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.MaxSideLength}"
									Visibility="Collapsed"
									RenderTransformOrigin=".5,.5"
									FlowDirection="LeftToRight">
									<Grid.Resources>
										<Style x:Key="ProgressRingEllipseStyle" TargetType="Ellipse">
											<Setter Property="Opacity" Value="0" />
											<Setter Property="HorizontalAlignment" Value="Left" />
											<Setter Property="VerticalAlignment" Value="Top" />
										</Style>
									</Grid.Resources>

									<VisualStateManager.VisualStateGroups>
										<VisualStateGroup x:Name="ActiveStates">
											<VisualState x:Name="Inactive" />
											<VisualState x:Name="Active">
												<Storyboard RepeatBehavior="Forever">
													<ObjectAnimationUsingKeyFrames Duration="0" Storyboard.TargetName="Ring" Storyboard.TargetProperty="Visibility">
														<DiscreteObjectKeyFrame KeyTime="0">
															<DiscreteObjectKeyFrame.Value>
																<Visibility>Visible</Visibility>
															</DiscreteObjectKeyFrame.Value>
														</DiscreteObjectKeyFrame>
													</ObjectAnimationUsingKeyFrames>
													<DoubleAnimationUsingKeyFrames Storyboard.TargetName="E1" Storyboard.TargetProperty="Opacity" BeginTime="0">
														<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.21" Value="1" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.22" Value="0" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.47" Value="0" />
													</DoubleAnimationUsingKeyFrames>
													<DoubleAnimationUsingKeyFrames Storyboard.TargetName="E2" Storyboard.TargetProperty="Opacity" BeginTime="00:00:00.167">
														<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.21" Value="1" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.22" Value="0" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.47" Value="0" />
													</DoubleAnimationUsingKeyFrames>
													<DoubleAnimationUsingKeyFrames Storyboard.TargetName="E3" Storyboard.TargetProperty="Opacity" BeginTime="00:00:00.334">
														<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.21" Value="1" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.22" Value="0" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.47" Value="0" />
													</DoubleAnimationUsingKeyFrames>
													<DoubleAnimationUsingKeyFrames Storyboard.TargetName="E4" Storyboard.TargetProperty="Opacity" BeginTime="00:00:00.501">
														<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.21" Value="1" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.22" Value="0" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.47" Value="0" />
													</DoubleAnimationUsingKeyFrames>
													<DoubleAnimationUsingKeyFrames Storyboard.TargetName="E5" Storyboard.TargetProperty="Opacity" BeginTime="00:00:00.668">
														<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.21" Value="1" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.22" Value="0" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.47" Value="0" />
													</DoubleAnimationUsingKeyFrames>
													<DoubleAnimationUsingKeyFrames Storyboard.TargetName="E6" Storyboard.TargetProperty="Opacity" BeginTime="00:00:00.835">
														<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.21" Value="1" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.22" Value="0" />
														<DiscreteDoubleKeyFrame KeyTime="0:0:3.47" Value="0" />
													</DoubleAnimationUsingKeyFrames>
													<DoubleAnimationUsingKeyFrames Storyboard.TargetName="E1R" BeginTime="0" Storyboard.TargetProperty="Angle">
														<SplineDoubleKeyFrame KeyTime="0" Value="-110" KeySpline="0.13,0.21,0.1,0.7" />
														<SplineDoubleKeyFrame KeyTime="0:0:0.433" Value="10" KeySpline="0.02,0.33,0.38,0.77" />
														<SplineDoubleKeyFrame KeyTime="0:0:1.2" Value="93" />
														<SplineDoubleKeyFrame KeyTime="0:0:1.617" Value="205" KeySpline="0.57,0.17,0.95,0.75" />
														<SplineDoubleKeyFrame KeyTime="0:0:2.017" Value="357" KeySpline="0,0.19,0.07,0.72" />
														<SplineDoubleKeyFrame KeyTime="0:0:2.783" Value="439" />
														<SplineDoubleKeyFrame KeyTime="0:0:3.217" Value="585" KeySpline="0,0,0.95,0.37" />
													</DoubleAnimationUsingKeyFrames>
													<DoubleAnimationUsingKeyFrames Storyboard.TargetName="E2R" BeginTime="00:00:00.167" Storyboard.TargetProperty="Angle">
														<SplineDoubleKeyFrame KeyTime="0" Value="-116" KeySpline="0.13,0.21,0.1,0.7" />
														<SplineDoubleKeyFrame KeyTime="0:0:0.433" Value="4" KeySpline="0.02,0.33,0.38,0.77" />
														<SplineDoubleKeyFrame KeyTime="0:0:1.2" Value="87" />
														<SplineDoubleKeyFrame KeyTime="0:0:1.617" Value="199" KeySpline="0.57,0.17,0.95,0.75" />
														<SplineDoubleKeyFrame KeyTime="0:0:2.017" Value="351" KeySpline="0,0.19,0.07,0.72" />
														<SplineDoubleKeyFrame KeyTime="0:0:2.783" Value="433" />
														<SplineDoubleKeyFrame KeyTime="0:0:3.217" Value="579" KeySpline="0,0,0.95,0.37" />
													</DoubleAnimationUsingKeyFrames>
													<DoubleAnimationUsingKeyFrames Storyboard.TargetName="E3R" BeginTime="00:00:00.334" Storyboard.TargetProperty="Angle">
														<SplineDoubleKeyFrame KeyTime="0" Value="-122" KeySpline="0.13,0.21,0.1,0.7" />
														<SplineDoubleKeyFrame KeyTime="0:0:0.433" Value="-2" KeySpline="0.02,0.33,0.38,0.77" />
														<SplineDoubleKeyFrame KeyTime="0:0:1.2" Value="81" />
														<SplineDoubleKeyFrame KeyTime="0:0:1.617" Value="193" KeySpline="0.57,0.17,0.95,0.75" />
														<SplineDoubleKeyFrame KeyTime="0:0:2.017" Value="345" KeySpline="0,0.19,0.07,0.72" />
														<SplineDoubleKeyFrame KeyTime="0:0:2.783" Value="427" />
														<SplineDoubleKeyFrame KeyTime="0:0:3.217" Value="573" KeySpline="0,0,0.95,0.37" />
													</DoubleAnimationUsingKeyFrames>
													<DoubleAnimationUsingKeyFrames Storyboard.TargetName="E4R" BeginTime="00:00:00.501" Storyboard.TargetProperty="Angle">
														<SplineDoubleKeyFrame KeyTime="0" Value="-128" KeySpline="0.13,0.21,0.1,0.7" />
														<SplineDoubleKeyFrame KeyTime="0:0:0.433" Value="-8" KeySpline="0.02,0.33,0.38,0.77" />
														<SplineDoubleKeyFrame KeyTime="0:0:1.2" Value="75" />
														<SplineDoubleKeyFrame KeyTime="0:0:1.617" Value="187" KeySpline="0.57,0.17,0.95,0.75" />
														<SplineDoubleKeyFrame KeyTime="0:0:2.017" Value="339" KeySpline="0,0.19,0.07,0.72" />
														<SplineDoubleKeyFrame KeyTime="0:0:2.783" Value="421" />
														<SplineDoubleKeyFrame KeyTime="0:0:3.217" Value="567" KeySpline="0,0,0.95,0.37" />
													</DoubleAnimationUsingKeyFrames>
													<DoubleAnimationUsingKeyFrames Storyboard.TargetName="E5R" BeginTime="00:00:00.668" Storyboard.TargetProperty="Angle">
														<SplineDoubleKeyFrame KeyTime="0" Value="-134" KeySpline="0.13,0.21,0.1,0.7" />
														<SplineDoubleKeyFrame KeyTime="0:0:0.433" Value="-14" KeySpline="0.02,0.33,0.38,0.77" />
														<SplineDoubleKeyFrame KeyTime="0:0:1.2" Value="69" />
														<SplineDoubleKeyFrame KeyTime="0:0:1.617" Value="181" KeySpline="0.57,0.17,0.95,0.75" />
														<SplineDoubleKeyFrame KeyTime="0:0:2.017" Value="331" KeySpline="0,0.19,0.07,0.72" />
														<SplineDoubleKeyFrame KeyTime="0:0:2.783" Value="415" />
														<SplineDoubleKeyFrame KeyTime="0:0:3.217" Value="561" KeySpline="0,0,0.95,0.37" />
													</DoubleAnimationUsingKeyFrames>
													<DoubleAnimationUsingKeyFrames Storyboard.TargetName="E6R" BeginTime="00:00:00.835" Storyboard.TargetProperty="Angle">
														<SplineDoubleKeyFrame KeyTime="0" Value="-140" KeySpline="0.13,0.21,0.1,0.7" />
														<SplineDoubleKeyFrame KeyTime="0:0:0.433" Value="-20" KeySpline="0.02,0.33,0.38,0.77" />
														<SplineDoubleKeyFrame KeyTime="0:0:1.2" Value="63" />
														<SplineDoubleKeyFrame KeyTime="0:0:1.617" Value="175" KeySpline="0.57,0.17,0.95,0.75" />
														<SplineDoubleKeyFrame KeyTime="0:0:2.017" Value="325" KeySpline="0,0.19,0.07,0.72" />
														<SplineDoubleKeyFrame KeyTime="0:0:2.783" Value="409" />
														<SplineDoubleKeyFrame KeyTime="0:0:3.217" Value="555" KeySpline="0,0,0.95,0.37" />
													</DoubleAnimationUsingKeyFrames>
												</Storyboard>
											</VisualState>
										</VisualStateGroup>

									</VisualStateManager.VisualStateGroups>
									<Canvas RenderTransformOrigin=".5,.5">
										<Canvas.RenderTransform>
											<RotateTransform x:Name="E1R" />
										</Canvas.RenderTransform>
										<Ellipse x:Name="E1"
										Style="{StaticResource ProgressRingEllipseStyle}"
										Width="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseDiameter}"
										Height="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseDiameter}"
										Margin="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseOffset}"
										Fill="{TemplateBinding Foreground}" />
									</Canvas>
									<Canvas RenderTransformOrigin=".5,.5">
										<Canvas.RenderTransform>
											<RotateTransform x:Name="E2R" />
										</Canvas.RenderTransform>
										<Ellipse x:Name="E2"
										Style="{StaticResource ProgressRingEllipseStyle}"
										Width="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseDiameter}"
										Height="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseDiameter}"
										Margin="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseOffset}"
										Fill="{TemplateBinding Foreground}" />
									</Canvas>
									<Canvas RenderTransformOrigin=".5,.5">
										<Canvas.RenderTransform>
											<RotateTransform x:Name="E3R" />
										</Canvas.RenderTransform>
										<Ellipse x:Name="E3"
										Style="{StaticResource ProgressRingEllipseStyle}"
										Width="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseDiameter}"
										Height="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseDiameter}"
										Margin="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseOffset}"
										Fill="{TemplateBinding Foreground}" />
									</Canvas>
									<Canvas RenderTransformOrigin=".5,.5">
										<Canvas.RenderTransform>
											<RotateTransform x:Name="E4R" />
										</Canvas.RenderTransform>
										<Ellipse x:Name="E4"
										Style="{StaticResource ProgressRingEllipseStyle}"
										Width="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseDiameter}"
										Height="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseDiameter}"
										Margin="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseOffset}"
										Fill="{TemplateBinding Foreground}" />
									</Canvas>
									<Canvas RenderTransformOrigin=".5,.5">
										<Canvas.RenderTransform>
											<RotateTransform x:Name="E5R" />
										</Canvas.RenderTransform>
										<Ellipse x:Name="E5"
										Style="{StaticResource ProgressRingEllipseStyle}"
										Width="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseDiameter}"
										Height="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseDiameter}"
										Margin="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseOffset}"
										Fill="{TemplateBinding Foreground}" />
									</Canvas>
									<Canvas RenderTransformOrigin=".5,.5" Visibility="Collapsed" x:Name="SixthCircle">
										<Canvas.RenderTransform>
											<RotateTransform x:Name="E6R" />
										</Canvas.RenderTransform>
										<Ellipse x:Name="E6"
										Style="{StaticResource ProgressRingEllipseStyle}"
										Width="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseDiameter}"
										Height="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseDiameter}"
										Margin="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.EllipseOffset}"
										Fill="{TemplateBinding Foreground}" />
									</Canvas>
								</Grid>
							</ControlTemplate>
						</Setter.Value>
					</Setter>
				</Style>
				"""),
		};
		await UITestHelper.Load(progressRing);

		var ring = (Grid)VisualTreeHelper.GetChild(progressRing, 0);
		var activeStates = VisualStateManager.GetVisualStateGroups(ring)[0];

		Verify.IsTrue(progressRing.IsActive);

		// IsActive set to true updates ProgressRing to Active state
		Verify.AreEqual("Active", activeStates.CurrentState?.Name);

		progressRing.IsActive = false;
		await TestServices.WindowHelper.WaitForIdle();

		// IsActive set to false updates ProgressRing to Inactive state
		Verify.AreEqual("Inactive", activeStates.CurrentState?.Name);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task ChangeValueTest()
	{
		var progressRing = new ProgressRing { IsIndeterminate = false };
		await UITestHelper.Load(progressRing);

		var rangeValue = GetRangeValueProvider(progressRing);
		Verify.AreEqual(0, rangeValue.Value);

		double oldValue = rangeValue.Value;

		ChangeValue(progressRing);

		double newValue = rangeValue.Value;
		double diff = Math.Abs(oldValue - newValue);

		// ProgressRing value changed
		Verify.IsGreaterThan(diff, 0.0);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task UpdateMinMaxTest()
	{
		var progressRing = new ProgressRing { IsIndeterminate = false };
		await UITestHelper.Load(progressRing);

		var rangeValue = GetRangeValueProvider(progressRing);

		double oldMinimumInputText = rangeValue.Minimum;
		double oldMaximumInputText = rangeValue.Maximum;

		UpdateMinMax(progressRing, minimum: 10, maximum: 15);

		double newMinimumInputText = rangeValue.Minimum;
		double newMaximumInputText = rangeValue.Maximum;

		Verify.AreNotEqual(oldMinimumInputText, newMinimumInputText, "Minimum updated");
		Verify.AreNotEqual(oldMaximumInputText, newMaximumInputText, "Maximum updated");

		// Updating Minimum and Maximum when Maximum < Minimum
		UpdateMinMax(progressRing, minimum: 10, maximum: 5);

		Verify.AreEqual(rangeValue.Minimum, rangeValue.Maximum, "Maximum updates to equal Minimum");

		// Updating Minimum and Maximum when Minimum > Value
		UpdateMinMax(progressRing, minimum: 15, maximum: 5);

		Verify.AreEqual(rangeValue.Value, rangeValue.Minimum, "Value updates to equal Minimum");
		Verify.AreEqual(rangeValue.Maximum, rangeValue.Minimum, "Maximum also updates to equal Minimum");

		// Updating Minimum and Maximum to be a decimal number
		UpdateMinMax(progressRing, minimum: 0.1, maximum: 1.1);

		double oldValue = rangeValue.Value;

		ChangeValue(progressRing);

		double newValue = rangeValue.Value;
		double diff = Math.Abs(oldValue - newValue);

		Verify.IsGreaterThan(diff, Convert.ToDouble(0), "Value of ProgressBar increments properly within range with decimal Minimum and Maximum");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task VerifyIndeterminateProgressRingDoesNotImplementRangeValuePattern()
	{
		var progressRing = new ProgressRing();
		AutomationProperties.SetName(progressRing, "TestProgressRing");
		await UITestHelper.Load(progressRing);

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(progressRing);

		// The page finds the ring by the name its peer reports while it is busy.
		Verify.AreEqual("Busy TestProgressRing", peer.GetName());

		Verify.IsNull(peer.GetPattern(PatternInterface.RangeValue));
	}

	[TestMethod]
	[RunsOnUIThread]
	[CombinatorialData]
	public async Task ProgressRingDefaultHeightShouldBe32(bool useFluent)
	{
		using (useFluent ? Disposable.Empty : StyleHelper.UseUwpStyles())
		{
			var grid = new Grid();
			grid.Width = 100;
			grid.Height = 100;
			var progressRing = new Microsoft.UI.Xaml.Controls.ProgressRing() { IsActive = true };
			grid.Children.Add(progressRing);
			RunOnUIThread.Execute(() => TestServices.WindowHelper.WindowContent = grid);

			await TestServices.WindowHelper.WaitForLoaded(progressRing);

			Assert.AreEqual(32, progressRing.ActualHeight);
			Assert.AreEqual(32, progressRing.ActualWidth);
		}
	}

	private static async Task VerifyStateTransitions(ProgressRing progressRing, Action verifyDeterminate = null)
	{
		var (layoutRoot, player) = GetTemplateParts(progressRing);
		var commonStatesGroup = VisualStateManager.GetVisualStateGroups(layoutRoot)[0];

		// Verify IsActive and IsIndeterminate property is set to true by default for testing
		Verify.IsTrue(progressRing.IsActive);
		Verify.IsTrue(progressRing.IsIndeterminate);

		// IsActive and IsIndeterminate set to true updates ProgressRing to Active state
		Verify.AreEqual("Active", commonStatesGroup.CurrentState?.Name);

		// Verity that opacity is 1 when Active
		Verify.AreEqual(1, layoutRoot.Opacity);

		// Verify Lottie animation is playing when in Active state
		Verify.IsTrue(player.IsPlaying);

		// IsActive set to True and IsIndeterminate set to false updates ProgressRing to DeterminateActive state
		progressRing.IsIndeterminate = false;
		await TestServices.WindowHelper.WaitForIdle();

		Verify.IsFalse(progressRing.IsIndeterminate);

		Verify.AreEqual("DeterminateActive", commonStatesGroup.CurrentState?.Name);
		verifyDeterminate?.Invoke();

		// Verify Lottie animation for Indeterminate player is not playing when in DeterminateActive state
		Verify.IsFalse(player.IsPlaying);

		// IsActive set to false updates ProgressRing to Inactive state
		progressRing.IsActive = false;
		await TestServices.WindowHelper.WaitForIdle();

		Verify.AreEqual("Inactive", commonStatesGroup.CurrentState?.Name);

		// Verify Lottie animation is not playing when in Inactive state
		Verify.IsFalse(player.IsPlaying);

		// Verity that opacity is 0 when Inactive
		await TestServices.WindowHelper.WaitFor(() => layoutRoot.Opacity == 0, message: "Opacity should reach 0 when Inactive");
	}

	private static (Grid LayoutRoot, AnimatedVisualPlayer Player) GetTemplateParts(ProgressRing progressRing)
	{
		var layoutRoot = (Grid)VisualTreeHelper.GetChild(progressRing, 0);
		var player = (AnimatedVisualPlayer)VisualTreeHelper.GetChild(layoutRoot, 0);
		return (layoutRoot, player);
	}

	private static IRangeValueProvider GetRangeValueProvider(ProgressRing progressRing)
		=> (IRangeValueProvider)FrameworkElementAutomationPeer.CreatePeerForElement(progressRing).GetPattern(PatternInterface.RangeValue);

	// ProgressRingPage.UpdateMinMax_Click
	private static void UpdateMinMax(ProgressRing progressRing, double minimum, double maximum)
	{
		progressRing.Maximum = maximum;
		progressRing.Minimum = minimum;
	}

	// ProgressRingPage.ChangeValue_Click
	private static void ChangeValue(ProgressRing progressRing)
	{
		if (progressRing.Value + 1 > progressRing.Maximum)
		{
			progressRing.Value = (int)(progressRing.Minimum + 0.5);
		}
		else
		{
			progressRing.Value += 1;
		}
	}
}
