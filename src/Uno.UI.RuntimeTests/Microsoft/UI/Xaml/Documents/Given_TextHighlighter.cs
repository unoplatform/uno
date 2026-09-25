using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents;

[TestClass]
public class Given_TextHighlighter
{
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24684")]
	public void When_Foreground_Is_Not_SolidColorBrush()
	{
		TextHighlighter sut = new();
		Assert.ThrowsExactly<ArgumentException>(() => sut.Foreground = new LinearGradientBrush());
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24684")]
	public void When_Background_Is_Not_SolidColorBrush()
	{
		TextHighlighter sut = new();
		Assert.ThrowsExactly<ArgumentException>(() => sut.Background = new LinearGradientBrush());
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24684")]
	public void When_Foreground_SetValue_Is_Not_SolidColorBrush()
	{
		TextHighlighter sut = new();
		Assert.ThrowsExactly<ArgumentException>(() => sut.SetValue(TextHighlighter.ForegroundProperty, new LinearGradientBrush()));
		Assert.IsNull(sut.Foreground, "The rejected value must not be committed");
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24684")]
	public void When_Background_SetValue_Is_Not_SolidColorBrush()
	{
		TextHighlighter sut = new();
		Assert.ThrowsExactly<ArgumentException>(() => sut.SetValue(TextHighlighter.BackgroundProperty, new LinearGradientBrush()));
		Assert.IsNull(sut.Background, "The rejected value must not be committed");
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24684")]
	public void When_Rejected_Brush_Keeps_Previous_Value()
	{
		var foreground = new SolidColorBrush(Colors.Red);
		var background = new SolidColorBrush(Colors.Blue);
		TextHighlighter sut = new() { Foreground = foreground, Background = background };

		Assert.ThrowsExactly<ArgumentException>(() => sut.SetValue(TextHighlighter.ForegroundProperty, new LinearGradientBrush()));
		Assert.ThrowsExactly<ArgumentException>(() => sut.SetValue(TextHighlighter.BackgroundProperty, new LinearGradientBrush()));

		Assert.AreSame(foreground, sut.Foreground);
		Assert.AreSame(background, sut.Background);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24684")]
	public void When_Null_Or_SolidColorBrush()
	{
		var foreground = new SolidColorBrush(Colors.Red);
		var background = new SolidColorBrush(Colors.Blue);
		TextHighlighter sut = new();

		sut.SetValue(TextHighlighter.ForegroundProperty, foreground);
		sut.Background = background;
		Assert.AreSame(foreground, sut.Foreground);
		Assert.AreSame(background, sut.Background);

		sut.Foreground = null;
		sut.SetValue(TextHighlighter.BackgroundProperty, null);
		Assert.IsNull(sut.Foreground);
		Assert.IsNull(sut.Background);
	}
}
