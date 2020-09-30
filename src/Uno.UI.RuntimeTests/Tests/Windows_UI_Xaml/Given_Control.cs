using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_Control
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_SetChildTemplateUsingVisualState()
		{
			var parent = (Button)XamlReader.Load(_when_SetChildTemplateUsingVisualState);
			TestServices.WindowHelper.WindowContent = parent;
			await TestServices.WindowHelper.WaitForIdle();

			var tb = parent.FindFirstChild<TextBlock>();

			Assert.IsNotNull(tb);
			Assert.AreEqual("Template loaded!", tb.Text);
		}

		private const string _when_SetChildTemplateUsingVisualState = @"
	<Button xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
		<Button.Template>
			<ControlTemplate TargetType=""ContentControl"">
				<Grid>
					<VisualStateManager.VisualStateGroups>
						<VisualStateGroup x:Name=""CommonStates"">
							<VisualState x:Name=""Normal"">
								<VisualState.Setters>
									<Setter  Target=""_sut.Template"">
										<Setter.Value>
											<ControlTemplate TargetType=""ContentControl"">
												<TextBlock Text=""Template loaded!"" />
											</ControlTemplate>
										</Setter.Value>
									</Setter>
								</VisualState.Setters>
							</VisualState>
						</VisualStateGroup>
					</VisualStateManager.VisualStateGroups>

					<ContentControl x:Name=""_sut"" />
				</Grid>
			</ControlTemplate>
		</Button.Template>
	</Button>";

	}
}
