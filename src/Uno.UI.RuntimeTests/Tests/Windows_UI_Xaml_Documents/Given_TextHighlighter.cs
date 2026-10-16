using System;
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
}
