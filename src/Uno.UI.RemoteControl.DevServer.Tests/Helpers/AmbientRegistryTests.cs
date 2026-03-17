using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RemoteControl.Host;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class AmbientRegistryTests
{
	[TestMethod]
	public void ResolveLocalApplicationDataPath_ReturnsAbsolutePath()
	{
		var path = AmbientRegistry.ResolveLocalApplicationDataPath();

		path.Should().NotBeNullOrWhiteSpace();
		Path.IsPathRooted(path).Should().BeTrue();
	}
}
