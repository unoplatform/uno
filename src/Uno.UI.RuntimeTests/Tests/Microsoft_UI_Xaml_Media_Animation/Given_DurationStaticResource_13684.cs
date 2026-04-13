using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Media_Animation
{
	[TestClass]
	public class Given_DurationStaticResource_13684
	{
		// Reproduction for https://github.com/unoplatform/uno/issues/13684
		// On WASM, a Duration declared in a top-level ResourceDictionary and referenced
		// from a DoubleAnimation inside a ControlTemplate via {StaticResource} was not
		// being applied — the animation never ran. This test wires up the same shape
		// (Application.Resources Duration -> ControlTemplate -> Storyboard) and asserts
		// the inner DoubleAnimation actually has the resolved Duration after template
		// application.
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_DoubleAnimation_Duration_Is_StaticResource_13684()
		{
			var control = new ContentControl();

			var resources = (ResourceDictionary)XamlReader.Load(
				"""
				<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
									xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					<Duration x:Key="STYLE_DURATION_VeryLongAnimation">00:00:05</Duration>
					<Style x:Key="MyStyle" TargetType="ContentControl">
						<Setter Property="Template">
							<Setter.Value>
								<ControlTemplate TargetType="ContentControl">
									<Grid>
										<Grid.Resources>
											<Storyboard x:Name="PART_Storyboard">
												<DoubleAnimation x:Name="PART_Animation"
																 Storyboard.TargetName="MyTransform"
																 Storyboard.TargetProperty="Rotation"
																 To="90"
																 Duration="{StaticResource STYLE_DURATION_VeryLongAnimation}" />
											</Storyboard>
										</Grid.Resources>
										<TextBlock Text="Hi">
											<TextBlock.RenderTransform>
												<CompositeTransform x:Name="MyTransform" />
											</TextBlock.RenderTransform>
										</TextBlock>
									</Grid>
								</ControlTemplate>
							</Setter.Value>
						</Setter>
					</Style>
				</ResourceDictionary>
				""");

			control.Resources = resources;
			control.Style = (Style)resources["MyStyle"];

			WindowHelper.WindowContent = control;
			await WindowHelper.WaitForLoaded(control);
			await WindowHelper.WaitForIdle();

			control.ApplyTemplate();

			var grid = (Grid)control.ContentTemplateRoot ?? control.FindFirstDescendant<Grid>();
			Assert.IsNotNull(grid, "Template root grid should be present.");

			var storyboard = (Storyboard)grid.Resources["PART_Storyboard"];
			Assert.IsNotNull(storyboard, "PART_Storyboard should be present in the template.");

			var animation = (DoubleAnimation)storyboard.Children[0];
			Assert.AreEqual(
				TimeSpan.FromSeconds(5),
				animation.Duration.TimeSpan,
				$"DoubleAnimation.Duration should resolve via {{StaticResource}} to 00:00:05. " +
				$"Actual = {animation.Duration}. See https://github.com/unoplatform/uno/issues/13684");
		}
	}
}
