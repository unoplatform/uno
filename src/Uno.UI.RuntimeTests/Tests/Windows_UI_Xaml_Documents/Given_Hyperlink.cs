using System;
using System.Linq;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using static Private.Infrastructure.TestServices;
using Uno.Disposables;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents;

[TestClass]
public class Given_Hyperlink
{
	[TestMethod]
	[RunsOnUIThread]
	[DataRow(true, false, "#FF0078D7")]
	[DataRow(false, false, "#FF0078D7")]
	[DataRow(true, true, "#FFA6D8FF")]
	[DataRow(false, true, "#FF004275")]
	public async Task TestSimpleHyperlink(bool useDark, bool useFluent, string expectedColorCode)
	{
		var expectedColor = (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedColorCode);
		using (useDark ? ThemeHelper.UseDarkTheme() : null)
		{
			using (useFluent ? Disposable.Empty : StyleHelper.UseUwpStyles())
			{
				var tb = (TextBlock)XamlReader.Load("""
				<TextBlock xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
					<Hyperlink>Hello</Hyperlink>
				</TextBlock>
				""");
				WindowHelper.WindowContent = tb;
				await WindowHelper.WaitForLoaded(tb);

				var run = (Run)((Hyperlink)tb.Inlines.Single()).Inlines.Single();
				Assert.AreEqual(expectedColor, ((SolidColorBrush)run.Foreground).Color);
			}
		}
	}

#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
#if __WASM__
	[Ignore("Visual states/Colors are handled by browser.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	[DataRow(true, false, "#FF0078D7", "#99FFFFFF")]
	[DataRow(false, false, "#FF0078D7", "#99000000")]
	[DataRow(true, true, "#FFA6D8FF", "#FFA6D8FF")]
	[DataRow(false, true, "#FF004275", "#FF002642")]
	public async Task TestHoveredHyperlink(bool useDark, bool useFluent, string expectedUnhoveredColorCode, string expectedHoveredColorCode)
	{
		var expectedUnhoveredColor = (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedUnhoveredColorCode);
		var expectedHoveredColor = (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedHoveredColorCode);
		using (useDark ? ThemeHelper.UseDarkTheme() : null)
		{
			using (useFluent ? Disposable.Empty : StyleHelper.UseUwpStyles())
			{
				var stackPanel = (StackPanel)XamlReader.Load("""
				<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
					<TextBlock>
						<Hyperlink>Hello</Hyperlink>
					</TextBlock>
					<TextBlock Text="Bye" />
				</StackPanel>
				""");

				WindowHelper.WindowContent = stackPanel;
				await WindowHelper.WaitForLoaded(stackPanel);
				await WindowHelper.WaitForIdle();

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				var tb1 = (TextBlock)stackPanel.Children[0];
				var tb2 = (TextBlock)stackPanel.Children[1];

				mouse.MoveTo(tb1.GetAbsoluteBounds().GetCenter());

				var run = (Run)((Hyperlink)tb1.Inlines.Single()).Inlines.Single();
				Assert.AreEqual(expectedHoveredColor, ((SolidColorBrush)run.Foreground).Color);

				mouse.MoveTo(tb2.GetAbsoluteBounds().GetCenter());
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(expectedUnhoveredColor, ((SolidColorBrush)run.Foreground).Color);
			}
		}
	}
#endif

#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
#if __WASM__
	[Ignore("Visual states/Colors are handled by browser.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	[DataRow(true, false, "#FF0078D7", "#99FFFFFF", "#66FFFFFF")]
	[DataRow(false, false, "#FF0078D7", "#99000000", "#66000000")]
	[DataRow(true, true, "#FFA6D8FF", "#FFA6D8FF", "#FF76B9ED")]
	[DataRow(false, true, "#FF004275", "#FF002642", "#FF005A9E")]
	public async Task TestPressedHyperlink(bool useDark, bool useFluent, string expectedUnhoveredColorCode, string expectedHoveredColorCode, string expectedPressedColorCode)
	{
		var expectedUnhoveredColor = (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedUnhoveredColorCode);
		var expectedHoveredColor = (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedHoveredColorCode);
		var expectedPressedColor = (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedPressedColorCode);

		using (useDark ? ThemeHelper.UseDarkTheme() : null)
		{
			using (useFluent ? Disposable.Empty : StyleHelper.UseUwpStyles())
			{
				var stackPanel = (StackPanel)XamlReader.Load("""
					<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
						<TextBlock>
							<Hyperlink>Hello</Hyperlink>
						</TextBlock>
						<TextBlock Text="Bye" />
					</StackPanel>
					""");

				WindowHelper.WindowContent = stackPanel;
				await WindowHelper.WaitForLoaded(stackPanel);
				await WindowHelper.WaitForIdle();

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				var tb1 = (TextBlock)stackPanel.Children[0];
				var tb2 = (TextBlock)stackPanel.Children[1];

				mouse.MoveTo(tb1.GetAbsoluteBounds().GetCenter());

				var run = (Run)((Hyperlink)tb1.Inlines.Single()).Inlines.Single();
				Assert.AreEqual(expectedHoveredColor, ((SolidColorBrush)run.Foreground).Color);

				mouse.Press();
				Assert.AreEqual(expectedPressedColor, ((SolidColorBrush)run.Foreground).Color);

				mouse.MoveTo(tb2.GetAbsoluteBounds().GetCenter());
				Assert.AreEqual(expectedPressedColor, ((SolidColorBrush)run.Foreground).Color);

				mouse.Release();
				Assert.AreEqual(expectedUnhoveredColor, ((SolidColorBrush)run.Foreground).Color);
			}
		}
	}
#endif

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(true, false, "#FF0078D7")]
	[DataRow(false, false, "#FF0078D7")]
	[DataRow(true, true, "#FFA6D8FF")]
	[DataRow(false, true, "#FF004275")]
	public async Task TestNotInheritedFromTextBlock(bool useDark, bool useFluent, string expectedColorCode)
	{
		var expectedColor = (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedColorCode);
		using (useDark ? ThemeHelper.UseDarkTheme() : null)
		{
			using (useFluent ? Disposable.Empty : StyleHelper.UseUwpStyles())
			{
				var tb = (TextBlock)XamlReader.Load("""
					<TextBlock TextDecorations='Strikethrough' Foreground='Red' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
						<Hyperlink>Not red</Hyperlink><Run>Red</Run>
					</TextBlock>
					""");
				WindowHelper.WindowContent = tb;
				await WindowHelper.WaitForLoaded(tb);

				Assert.AreEqual(2, tb.Inlines.Count);
				var hyperlink = (Hyperlink)tb.Inlines[0];
				var hyperlinkRun = (Run)hyperlink.Inlines.Single();
				var redRun = (Run)tb.Inlines[1];

				Assert.AreEqual(Colors.Red, ((SolidColorBrush)redRun.Foreground).Color);
				Assert.AreEqual(TextDecorations.Strikethrough, redRun.TextDecorations);

				Assert.AreEqual(expectedColor, ((SolidColorBrush)hyperlinkRun.Foreground).Color);
				Assert.AreEqual(TextDecorations.Underline, hyperlinkRun.TextDecorations);
			}
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task TestRespectHyperlinkForeground()
	{
		const string HyperlinkForeground = nameof(HyperlinkForeground);

		var appLevelResources = new ResourceDictionary();
		appLevelResources[HyperlinkForeground] = new SolidColorBrush(Colors.Orange);
		using (StyleHelper.UseAppLevelResources(appLevelResources))
		{
			var tb = (TextBlock)XamlReader.Load("""
						<TextBlock TextDecorations='Strikethrough' Foreground='Red' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
							<Hyperlink>Not red</Hyperlink><Run>Red</Run>
						</TextBlock>
						""");
			WindowHelper.WindowContent = tb;
			await WindowHelper.WaitForLoaded(tb);

			Assert.AreEqual(2, tb.Inlines.Count);
			var hyperlink = (Hyperlink)tb.Inlines[0];
			var hyperlinkRun = (Run)hyperlink.Inlines.Single();
			var redRun = (Run)tb.Inlines[1];

			Assert.AreEqual(Colors.Red, ((SolidColorBrush)redRun.Foreground).Color);
			Assert.AreEqual(TextDecorations.Strikethrough, redRun.TextDecorations);

			Assert.AreEqual(Colors.Orange, ((SolidColorBrush)hyperlinkRun.Foreground).Color);
			Assert.AreEqual(TextDecorations.Underline, hyperlinkRun.TextDecorations);

		}
	}
}
