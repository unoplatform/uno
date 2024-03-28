#nullable enable

using Uno.UI.Helpers;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

public class BaseHotReloadTestClass
{
	[TestInitialize]
	public void InitHotReload()
	{
		TypeMappings.ClearMappings();

		// Make sure type mappings is running
		TypeMappings.Resume();
	}
}
