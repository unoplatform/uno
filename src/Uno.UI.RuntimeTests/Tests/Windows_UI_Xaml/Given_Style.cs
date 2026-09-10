using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	// The attribute is required when running WinUI. See:
	// https://github.com/microsoft/microsoft-ui-xaml/issues/4723#issuecomment-812753123
	[Bindable]
	public sealed partial class ThrowingElement : FrameworkElement
	{
		public ThrowingElement() => throw new Exception("Inner exception");
	}

	[TestClass]
	public class Given_Style
	{
#if HAS_UNO
		private bool _previousDeferOverriddenSetterValues;

		[TestInitialize]
		public void Initialize()
		{
			_previousDeferOverriddenSetterValues = FeatureConfiguration.Style.DeferOverriddenSetterValues;
			FeatureConfiguration.Style.DeferOverriddenSetterValues = true;
		}

		[TestCleanup]
		public void Cleanup()
			=> FeatureConfiguration.Style.DeferOverriddenSetterValues = _previousDeferOverriddenSetterValues;
#endif

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_StyleFailsToApply()
		{
			var controlTemplate = (ControlTemplate)XamlReader.Load("""
				<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
								 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
								 xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml">
					<local:ThrowingElement />
				</ControlTemplate>
				""");

			var style = new Style()
			{
				Setters =
				{
					new Setter(ContentControl.TemplateProperty, controlTemplate)
				}
			};

			// This shouldn't throw.
			_ = new ContentControl() { Style = style };
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15460")]
#if __ANDROID__
		[Ignore("Doesn't pass in CI on Android")]
#endif
		public async Task When_ImplicitStyle()
		{
			var implicitStyle = new Style()
			{
				Setters =
				{
					new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch)
				},
				TargetType = typeof(ContentControl),
			};

			var explicitStyle = new Style()
			{
				TargetType = typeof(ContentControl),
			};

			var cc = new ContentControl() { Width = 100, Height = 100 };

			// On Android and iOS, ContentControl fails to load if it doesn't have content.
			cc.Content = new Border() { Width = 100, Height = 100 };

			Assert.AreEqual(HorizontalAlignment.Center, cc.HorizontalContentAlignment);

			cc.Resources.Add(typeof(ContentControl), implicitStyle);
			await UITestHelper.Load(cc);

			Assert.AreEqual(HorizontalAlignment.Stretch, cc.HorizontalContentAlignment);

			cc.Style = explicitStyle;

			Assert.AreEqual(HorizontalAlignment.Left, cc.HorizontalContentAlignment);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Style_Flows_To_Popup()
		{
			var page = new StyleFlowToPopup();
			TestServices.WindowHelper.WindowContent = page;
			await UITestHelper.Load(page);

			var foreground = (SolidColorBrush)page.GridTextBlock.Foreground;
			Assert.AreEqual(Microsoft.UI.Colors.Red, foreground.Color);

			page.ShowPopup();

			await TestServices.WindowHelper.WaitFor(() => VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).Count > 0);

			var popupForeground = (SolidColorBrush)page.PopupTextBlock.Foreground;
			Assert.AreEqual(Microsoft.UI.Colors.Red, popupForeground.Color);
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Overridden_Setter_Is_Not_Materialized()
		{
#if HAS_UNO
			var count = 0;
			SetterValueProviderHandler provider = () =>
			{
				count++;
				return "fromStyle";
			};

			var style = new Style(typeof(Border))
			{
				Setters =
				{
					new Setter(FrameworkElement.TagProperty, provider)
				}
			};

			var border = new Border { Width = 50, Height = 50, Tag = "local" };

			try
			{
				await UITestHelper.Load(border);

				border.Style = style;
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("local", border.Tag);
				Assert.AreEqual(0, count, "The overridden setter value must not be materialized.");

				border.ClearValue(FrameworkElement.TagProperty);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("fromStyle", border.Tag);
				Assert.AreEqual(1, count, "The winning setter value must be materialized exactly once.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
#else
			await Task.CompletedTask;
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Explicit_Style_Cleared_Then_BuiltIn_Template_Is_Materialized()
		{
			var template = (ControlTemplate)XamlReader.Load("""
				<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
								 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
								 TargetType="Button">
					<Border x:Name="CustomRoot" Width="50" Height="50" Background="Red" />
				</ControlTemplate>
				""");

			var style = new Style(typeof(Button))
			{
				Setters =
				{
					new Setter(Control.TemplateProperty, template)
				}
			};

			var button = new Button { Content = "Deferred", Style = style };

			try
			{
				await UITestHelper.Load(button, x => x.IsLoaded);

				Assert.AreSame(template, button.Template);
				Assert.IsNotNull(FindDescendantByName(button, "CustomRoot"));

				// Clearing the explicit style must fall back to the built-in style template,
				// which was never materialized while the explicit style was winning.
				button.ClearValue(FrameworkElement.StyleProperty);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsNotNull(button.Template);
				Assert.AreNotSame(template, button.Template);
				Assert.IsNull(FindDescendantByName(button, "CustomRoot"));
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_ThemeResource_Setter_Overridden_Then_Theme_Still_Refreshes()
		{
			// The brushes live at the application level so that both the resource binding registered when the
			// setter is applied and the static retrieval performed when the overriding value is cleared resolve
			// them, independently of the (scope-less) runtime XAML reader.
			var appResources = Application.Current.Resources;
			appResources.ThemeDictionaries.TryGetValue("Light", out var previousLight);
			appResources.ThemeDictionaries.TryGetValue("Dark", out var previousDark);

			appResources.ThemeDictionaries["Light"] = new ResourceDictionary
			{
				["DeferredSetterTestBrush"] = new SolidColorBrush(Microsoft.UI.Colors.Green)
			};
			appResources.ThemeDictionaries["Dark"] = new ResourceDictionary
			{
				["DeferredSetterTestBrush"] = new SolidColorBrush(Microsoft.UI.Colors.Red)
			};

			try
			{
				using var _ = ThemeHelper.UseApplicationLightTheme();
				await TestServices.WindowHelper.WaitForIdle();

				var root = (Grid)XamlReader.Load("""
					<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
						<Grid.Resources>
							<Style x:Key="DeferredSetterTestStyle" TargetType="Border">
								<Setter Property="Background" Value="{ThemeResource DeferredSetterTestBrush}" />
							</Style>
						</Grid.Resources>
						<Border x:Name="border"
								Width="50"
								Height="50"
								Background="Blue"
								Style="{StaticResource DeferredSetterTestStyle}" />
					</Grid>
					""");

				var border = (Border)root.FindName("border");

				TestServices.WindowHelper.WindowContent = root;
				await TestServices.WindowHelper.WaitForLoaded(root);
				await TestServices.WindowHelper.WaitForIdle();

				// The local value wins over the ThemeResource-backed setter.
				Assert.AreEqual(Microsoft.UI.Colors.Blue, ((SolidColorBrush)border.Background).Color);

				border.ClearValue(Border.BackgroundProperty);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(Microsoft.UI.Colors.Green, ((SolidColorBrush)border.Background).Color);

				using (ThemeHelper.UseApplicationDarkTheme())
				{
					await TestServices.WindowHelper.WaitForIdle();

					Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)border.Background).Color);
				}

				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(Microsoft.UI.Colors.Green, ((SolidColorBrush)border.Background).Color);
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;

				if (previousLight is null)
				{
					appResources.ThemeDictionaries.Remove("Light");
				}
				else
				{
					appResources.ThemeDictionaries["Light"] = previousLight;
				}

				if (previousDark is null)
				{
					appResources.ThemeDictionaries.Remove("Dark");
				}
				else
				{
					appResources.ThemeDictionaries["Dark"] = previousDark;
				}
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Style_Replaced_While_Overridden_Then_New_Style_Wins()
		{
			var styleA = new Style(typeof(Border))
			{
				Setters = { new Setter(FrameworkElement.TagProperty, "A") }
			};

			var styleB = new Style(typeof(Border))
			{
				Setters = { new Setter(FrameworkElement.TagProperty, "B") }
			};

			var border = new Border { Width = 50, Height = 50, Tag = "local" };

			try
			{
				await UITestHelper.Load(border);

				border.Style = styleA;
				border.Style = styleB;
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("local", border.Tag);

				border.ClearValue(FrameworkElement.TagProperty);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("B", border.Tag);

				border.ClearValue(FrameworkElement.StyleProperty);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsNull(border.Tag);
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		private static FrameworkElement FindDescendantByName(DependencyObject parent, string name)
		{
			for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				if (child is FrameworkElement { Name: var childName } element && childName == name)
				{
					return element;
				}

				if (FindDescendantByName(child, name) is { } descendant)
				{
					return descendant;
				}
			}

			return null;
		}
	}
}
