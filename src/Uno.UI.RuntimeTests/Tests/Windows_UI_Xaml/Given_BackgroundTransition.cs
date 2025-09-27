using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml.Markup;
using Uno.Extensions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_BackgroundTransition
{
#if !__SKIA__
	[Ignore]
#endif
	[TestMethod]
	[DataRow(typeof(Grid))]
	[DataRow(typeof(StackPanel))]
	[DataRow(typeof(Border))]
	[DataRow(typeof(ContentPresenter))]
	[RequiresFullWindow] // https://github.com/unoplatform/uno/issues/17470
	public async Task When_Has_Brush_Transition(Type type)
	{
		if (OperatingSystem.IsAndroid())
		{
			// This test is generally flaky due to its nature.
			Assert.Inconclusive("Animations are flaky.");
		}

		var control = (FrameworkElement)Activator.CreateInstance(type);

		control.Width = 200;
		control.Height = 200;

		Action<Brush> setBackground = null;
		Action<BrushTransition> setTransition = null;
		if (control is Panel panel)
		{
			setBackground = b => panel.Background = b;
			setTransition = t => panel.BackgroundTransition = t;
		}
		else if (control is Border border)
		{
			setBackground = b => border.Background = b;
			setTransition = t => border.BackgroundTransition = t;
		}
		else if (control is ContentPresenter contentPresenter)
		{
			setBackground = b => contentPresenter.Background = b;
			setTransition = t => contentPresenter.BackgroundTransition = t;
		}
		else
		{
			Assert.Fail("Unexpected input");
		}

		setBackground(new SolidColorBrush(Microsoft.UI.Colors.Red));

		setTransition(new BrushTransition { Duration = TimeSpan.FromMilliseconds(2000) });

		await UITestHelper.Load(control);

		setBackground(new SolidColorBrush(Microsoft.UI.Colors.Blue));

		await Task.Delay(950);

		var bitmap = await UITestHelper.ScreenShot(control);

		ImageAssert.HasColorAt(bitmap, new Point(100, 100), Color.FromArgb(255, 127, 0, 127), tolerance: 30);
	}

#if !__SKIA__
	[Ignore]
#endif
	[TestMethod]
	// Test is flaky on iOS https://github.com/unoplatform/uno-private/issues/797
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaUIKit)]
	public async Task When_Animation_With_Brush_Transition()
	{
		var SUT = (Button)XamlReader.Load(
		"""
		<Button xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				Width="48"
				Height="48"
				Content=" ">
		  <Button.Style>
		    <Style x:Key="DefaultButtonStyle"
		           TargetType="Button">
		      <Setter Property="Template">
		        <Setter.Value>
		          <ControlTemplate TargetType="Button">
		            <Grid>
		              <ContentPresenter x:Name="ContentPresenter"
		                                Background="Blue"
		                                Content="{TemplateBinding Content}">
		
		                <ContentPresenter.BackgroundTransition>
		                  <BrushTransition Duration="0:0:2" />
		                </ContentPresenter.BackgroundTransition>
		              </ContentPresenter>
		
		              <VisualStateManager.VisualStateGroups>
		                <VisualStateGroup x:Name="CommonStates">
		                  <VisualState x:Name="Normal" />
		
		                  <VisualState x:Name="PointerOver">
		                    <Storyboard>
		                      <ObjectAnimationUsingKeyFrames Storyboard.TargetName="ContentPresenter"
		                                                     Storyboard.TargetProperty="Background">
		                        <DiscreteObjectKeyFrame KeyTime="0" Value="Red" />
		                      </ObjectAnimationUsingKeyFrames>
		                    </Storyboard>
		                  </VisualState>
		                </VisualStateGroup>
		              </VisualStateManager.VisualStateGroups>
		            </Grid>
		          </ControlTemplate>
		        </Setter.Value>
		      </Setter>
		    </Style>
		  </Button.Style>
		</Button>                                  
		""");

		await UITestHelper.Load(SUT);

		VisualStateManager.GoToState(SUT, "PointerOver", true);

		// Instantly Red even though the transition is 2 seconds long
		var bitmap = await UITestHelper.ScreenShot(SUT);
		ImageAssert.HasColorAt(bitmap, new Point(bitmap.Width / 2, bitmap.Height / 2), Microsoft.UI.Colors.Red);

		VisualStateManager.GoToState(SUT, "Normal", true);
		await Task.Delay(1000);

		bitmap = await UITestHelper.ScreenShot(SUT);
		ImageAssert.HasColorAt(bitmap, new Point(bitmap.Width / 2, bitmap.Height / 2), Color.FromArgb(255, 127, 0, 127), tolerance: 20);
	}
}
