using Microsoft.UI.Composition;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.CompositionTests;

[UITestClass]
public class Given_CompositionGradientBrush
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26408")]
	public void When_LinearGradientBrush_Created_MappingMode_Is_Relative()
	{
		var compositor = Compositor.GetSharedCompositor();
		var brush = compositor.CreateLinearGradientBrush();

		Assert.AreEqual(CompositionMappingMode.Relative, brush.MappingMode);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26408")]
	public void When_RadialGradientBrush_Created_MappingMode_Is_Relative()
	{
		var compositor = Compositor.GetSharedCompositor();
		var brush = compositor.CreateRadialGradientBrush();

		Assert.AreEqual(CompositionMappingMode.Relative, brush.MappingMode);
	}
}
