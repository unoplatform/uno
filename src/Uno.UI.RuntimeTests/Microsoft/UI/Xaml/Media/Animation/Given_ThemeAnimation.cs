using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.RuntimeTests.Extensions;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ThemeAnimation
	{
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/8339")]
		public void When_FadeInThemeAnimation_Reparented_To_Timeline()
		{
			// Reparent (7.0 breaking change): the *ThemeAnimation family derives directly
			// from Timeline, matching WinUI, instead of Uno's legacy DoubleAnimation base.
			Assert.AreEqual(typeof(Timeline), typeof(FadeInThemeAnimation).BaseType);
			Assert.AreNotEqual(typeof(DoubleAnimation), typeof(FadeInThemeAnimation).BaseType);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/8339")]
		public void When_FadeOutThemeAnimation_Reparented_To_Timeline()
		{
			Assert.AreEqual(typeof(Timeline), typeof(FadeOutThemeAnimation).BaseType);
			Assert.AreNotEqual(typeof(DoubleAnimation), typeof(FadeOutThemeAnimation).BaseType);
		}

		[TestMethod]
		public async Task When_FadeOutThemeAnimation_Runs_Then_Opacity_Is_Unchanged()
		{
			var (control, target) = await LoadState("""<FadeOutThemeAnimation TargetName="Target" />""");

			VisualStateManager.GoToState(control, "Animated", false);
			await Task.Delay(1000);
			await TestServices.WindowHelper.WaitForIdle();

			// The theme animations animate the transition target of the element, not its Opacity.
			Assert.AreEqual(1.0, target.Opacity);
#if HAS_UNO
			Assert.AreEqual(0.0, (double)target.GetValue(UIElement.TransitionTargetOpacityProperty), 0.05);
#endif
		}

		[TestMethod]
		public async Task When_FadeInThemeAnimation_And_Opacity_Animation_Share_A_Storyboard_Then_Both_Run()
		{
			// Like the MultipleDraggingPrimary state of ListViewItem, which animates the Opacity of the elements it fades in.
			var (control, target) = await LoadState("""
				<DoubleAnimation Storyboard.TargetName="Target" Storyboard.TargetProperty="Opacity" To="0.5" Duration="0" />
				<FadeInThemeAnimation TargetName="Target" />
				""");

			VisualStateManager.GoToState(control, "Animated", false);
			await TestServices.WindowHelper.WaitFor(() => target.Opacity == 0.5, message: "the Opacity animation should run alongside the theme animation");
		}

		private static async Task<(Control control, Border target)> LoadState(string animations)
		{
			var control = (ContentControl)Microsoft.UI.Xaml.Markup.XamlReader.Load($$"""
				<ContentControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
								xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					<ContentControl.Template>
						<ControlTemplate TargetType="ContentControl">
							<Grid>
								<VisualStateManager.VisualStateGroups>
									<VisualStateGroup x:Name="States">
										<VisualState x:Name="Normal" />
										<VisualState x:Name="Animated">
											<Storyboard>
												{{animations}}
											</Storyboard>
										</VisualState>
									</VisualStateGroup>
								</VisualStateManager.VisualStateGroups>
								<Border x:Name="Target" Width="100" Height="100" Background="Pink" />
							</Grid>
						</ControlTemplate>
					</ContentControl.Template>
				</ContentControl>
				""");

			await UITestHelper.Load(control);

			var target = (Border)((FrameworkElement)Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(control, 0)).FindName("Target");
			return (control, target);
		}
	}
}
