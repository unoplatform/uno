using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Microsoft.UI;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
#if WINAPPSDK
using Uno.UI.Extensions;
#else
using Uno.UI;
#endif
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_FrameworkElement_ThemeResources
	{
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		// Not [RequiresFullWindow]/UseApplicationLightTheme: this test sets WindowContent=null mid-run,
		// which nulls XamlRoot.Content in full-window mode (breaking UseDarkTheme). It runs embedded and
		// relies on the host staying Light (IsAppThemeLight + the UseDarkTheme restore fix) for determinism.
		public async Task When_Detached_From_Window_While_Theme_Changed()
		{
			var SUT = new Button { Content = "Ye button" };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(Color.FromArgb(0xE4, 0, 0, 0), (SUT.Foreground as SolidColorBrush)?.Color);

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			using (ThemeHelper.UseDarkTheme())
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				Assert.AreEqual(Colors.White, (SUT.Foreground as SolidColorBrush)?.Color);
			}
		}

		[TestMethod]
		public async Task When_Styled_And_Not_Loaded_While_Theme_Changed()
		{
			var buttonUserControl = new ButtonUserControl();

			using (ThemeHelper.UseDarkTheme())
			{
				WindowHelper.WindowContent = buttonUserControl;
				await WindowHelper.WaitForLoaded(buttonUserControl);

				Assert.AreEqual(Colors.White, (buttonUserControl.MyButton.Foreground as SolidColorBrush)?.Color);
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_ComboBox_Theme_Changed()
		{
			var comboBox = new ComboBox() { PlaceholderText = "combo" };
			WindowHelper.WindowContent = comboBox;
			await WindowHelper.WaitForLoaded(comboBox);

			comboBox.ItemsSource = "abcdef".ToArray();

			using (ThemeHelper.UseDarkTheme())
			{
				await OpenAndCheckComboBox(comboBox, Colors.White, Color.FromArgb(255, 44, 44, 44));

			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		[RequiresFullWindow]
		public async Task When_ComboBox_Theme_Changed_After_First_Open()
		{
			using var _ = ThemeHelper.UseApplicationLightTheme();
			var comboBox = new ComboBox() { PlaceholderText = "combo" };
			WindowHelper.WindowContent = comboBox;
			await WindowHelper.WaitForLoaded(comboBox);

			comboBox.ItemsSource = "abcdef".ToArray();

			await OpenAndCheckComboBox(comboBox, Color.FromArgb(228, 0, 0, 0), Color.FromArgb(255, 249, 249, 249));

			using (ThemeHelper.UseDarkTheme())
			{
				await OpenAndCheckComboBox(comboBox, Colors.White, Color.FromArgb(255, 44, 44, 44));

			}
		}

		[TestMethod]
		public async Task When_Nearer_Ancestor_Overrides_Style_And_Template_ThemeResource()
		{
			var root = (Grid)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"""
				<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				      RequestedTheme="Light">
					<Grid.Resources>
						<ResourceDictionary>
							<ResourceDictionary.ThemeDictionaries>
								<ResourceDictionary x:Key="Light">
									<SolidColorBrush x:Key="ProbeBrush" Color="#FFFF0000" />
								</ResourceDictionary>
								<ResourceDictionary x:Key="Dark">
									<SolidColorBrush x:Key="ProbeBrush" Color="#FF800000" />
								</ResourceDictionary>
							</ResourceDictionary.ThemeDictionaries>
							<Style TargetType="ContentControl">
								<Setter Property="Background" Value="{ThemeResource ProbeBrush}" />
							</Style>
							<ControlTemplate x:Key="ProbeTemplate" TargetType="ContentControl">
								<Border Width="20" Height="20" Background="{ThemeResource ProbeBrush}" />
							</ControlTemplate>
						</ResourceDictionary>
					</Grid.Resources>
					<StackPanel>
						<StackPanel.Resources>
							<ResourceDictionary>
								<ResourceDictionary.ThemeDictionaries>
									<ResourceDictionary x:Key="Light">
										<SolidColorBrush x:Key="ProbeBrush" Color="#FF00FF00" />
									</ResourceDictionary>
									<ResourceDictionary x:Key="Dark">
										<SolidColorBrush x:Key="ProbeBrush" Color="#FF008000" />
									</ResourceDictionary>
								</ResourceDictionary.ThemeDictionaries>
							</ResourceDictionary>
						</StackPanel.Resources>
						<ContentControl x:Name="Styled" />
						<ContentControl x:Name="Templated" Template="{StaticResource ProbeTemplate}" />
					</StackPanel>
				</Grid>
				""");

			WindowHelper.WindowContent = root;
			await WindowHelper.WaitForLoaded(root);
			await WindowHelper.WaitForIdle();

			var styled = (ContentControl)root.FindName("Styled");
			var templatePart = (Border)VisualTreeHelper.GetChild((ContentControl)root.FindName("Templated"), 0);

			// The nearest dictionary wins over the one the style and the template were declared in.
			Assert.AreEqual(Color.FromArgb(0xFF, 0x00, 0xFF, 0x00), (styled.Background as SolidColorBrush)?.Color, "Style setter, Light");
			Assert.AreEqual(Color.FromArgb(0xFF, 0x00, 0xFF, 0x00), (templatePart.Background as SolidColorBrush)?.Color, "Template part, Light");

			root.RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Dark;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(Color.FromArgb(0xFF, 0x00, 0x80, 0x00), (styled.Background as SolidColorBrush)?.Color, "Style setter, Dark");
			Assert.AreEqual(Color.FromArgb(0xFF, 0x00, 0x80, 0x00), (templatePart.Background as SolidColorBrush)?.Color, "Template part, Dark");
		}

		[TestMethod]
		public async Task When_Styled_Element_Moves_Under_Overriding_Ancestor()
		{
			var root = (Grid)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"""
				<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				      RequestedTheme="Light">
					<Grid.Resources>
						<ResourceDictionary>
							<ResourceDictionary.ThemeDictionaries>
								<ResourceDictionary x:Key="Light">
									<SolidColorBrush x:Key="ProbeBrush" Color="#FFFF0000" />
								</ResourceDictionary>
								<ResourceDictionary x:Key="Dark">
									<SolidColorBrush x:Key="ProbeBrush" Color="#FF800000" />
								</ResourceDictionary>
							</ResourceDictionary.ThemeDictionaries>
							<Style TargetType="ContentControl">
								<Setter Property="Background" Value="{ThemeResource ProbeBrush}" />
							</Style>
						</ResourceDictionary>
					</Grid.Resources>
					<StackPanel>
						<StackPanel x:Name="Plain">
							<ContentControl x:Name="Styled" Width="20" Height="20" />
						</StackPanel>
						<StackPanel x:Name="Overriding">
							<StackPanel.Resources>
								<ResourceDictionary>
									<ResourceDictionary.ThemeDictionaries>
										<ResourceDictionary x:Key="Light">
											<SolidColorBrush x:Key="ProbeBrush" Color="#FF00FF00" />
										</ResourceDictionary>
										<ResourceDictionary x:Key="Dark">
											<SolidColorBrush x:Key="ProbeBrush" Color="#FF008000" />
										</ResourceDictionary>
									</ResourceDictionary.ThemeDictionaries>
								</ResourceDictionary>
							</StackPanel.Resources>
						</StackPanel>
					</StackPanel>
				</Grid>
				""");

			WindowHelper.WindowContent = root;
			await WindowHelper.WaitForLoaded(root);
			await WindowHelper.WaitForIdle();

			var styled = (ContentControl)root.FindName("Styled");
			Assert.AreEqual(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00), (styled.Background as SolidColorBrush)?.Color);

			((StackPanel)root.FindName("Plain")).Children.Remove(styled);
			((StackPanel)root.FindName("Overriding")).Children.Add(styled);
			await WindowHelper.WaitFor(() => styled.IsLoaded);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(Color.FromArgb(0xFF, 0x00, 0xFF, 0x00), (styled.Background as SolidColorBrush)?.Color, "Re-entering resolves against the new ancestors");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		// Uno re-resolves at Loading here; WinUI's Enter returns early as the element keeps its own theme.
		public async Task When_Element_With_Own_Theme_Moves_Under_Overriding_Ancestor()
		{
			var root = (Grid)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"""
				<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				      RequestedTheme="Light">
					<Grid.Resources>
						<ResourceDictionary>
							<ResourceDictionary.ThemeDictionaries>
								<ResourceDictionary x:Key="Light">
									<SolidColorBrush x:Key="ProbeBrush" Color="#FFFF0000" />
								</ResourceDictionary>
								<ResourceDictionary x:Key="Dark">
									<SolidColorBrush x:Key="ProbeBrush" Color="#FF800000" />
								</ResourceDictionary>
							</ResourceDictionary.ThemeDictionaries>
							<Style TargetType="ContentControl">
								<Setter Property="Background" Value="{ThemeResource ProbeBrush}" />
							</Style>
						</ResourceDictionary>
					</Grid.Resources>
					<StackPanel>
						<StackPanel x:Name="Plain">
							<ContentControl x:Name="Styled" Width="20" Height="20" RequestedTheme="Dark" />
						</StackPanel>
						<StackPanel x:Name="Overriding">
							<StackPanel.Resources>
								<ResourceDictionary>
									<ResourceDictionary.ThemeDictionaries>
										<ResourceDictionary x:Key="Light">
											<SolidColorBrush x:Key="ProbeBrush" Color="#FF00FF00" />
										</ResourceDictionary>
										<ResourceDictionary x:Key="Dark">
											<SolidColorBrush x:Key="ProbeBrush" Color="#FF008000" />
										</ResourceDictionary>
									</ResourceDictionary.ThemeDictionaries>
								</ResourceDictionary>
							</StackPanel.Resources>
						</StackPanel>
					</StackPanel>
				</Grid>
				""");

			WindowHelper.WindowContent = root;
			await WindowHelper.WaitForLoaded(root);

			// A theme walk establishes the themes the Enter of the moved element compares.
			root.RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Dark;
			await WindowHelper.WaitForIdle();
			root.RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Light;
			await WindowHelper.WaitForIdle();

			var styled = (ContentControl)root.FindName("Styled");
			Assert.AreEqual(Color.FromArgb(0xFF, 0x80, 0x00, 0x00), (styled.Background as SolidColorBrush)?.Color);

			((StackPanel)root.FindName("Plain")).Children.Remove(styled);
			((StackPanel)root.FindName("Overriding")).Children.Add(styled);
			await WindowHelper.WaitFor(() => styled.IsLoaded);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(Color.FromArgb(0xFF, 0x00, 0x80, 0x00), (styled.Background as SolidColorBrush)?.Color);
		}

		private static async Task OpenAndCheckComboBox(ComboBox comboBox, Color expectedForeground, Color expectedBackground)
		{
			try
			{
				comboBox.IsDropDownOpen = true;
				var firstItem = await WindowHelper.WaitForNonNull(() => comboBox.ContainerFromIndex(1) as ComboBoxItem);
				await WindowHelper.WaitForLoaded(firstItem);

				Assert.AreEqual(expectedForeground, (firstItem.Foreground as SolidColorBrush)?.Color);

				var popup = comboBox.FindFirstChild<Popup>();
				var popupBorder = popup.Child as Border;
				Assert.IsNotNull(popupBorder);
				var acrylicBrush = popupBorder.Background as AcrylicBrush;
				var color = acrylicBrush.FallbackColor;
				var actualColor = Color.FromArgb((byte)(color.A * acrylicBrush.Opacity), color.R, color.G, color.B);
				Assert.AreEqual(expectedBackground, actualColor);
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
				await WindowHelper.WaitForIdle();
			}
		}
	}
}
