#if __SKIA__
using Microsoft.UI.Composition;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
public class Given_Compositor
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_Skia_Backend_Then_IsSoftwareRenderer_Populated()
	{
		// Every Skia render backend must report whether it rasterizes on the CPU as soon as
		// its renderer is selected; effect brushes rely on this while recording the scene.
		Assert.IsNotNull(Compositor.GetSharedCompositor().IsSoftwareRenderer);
	}
}
#endif
