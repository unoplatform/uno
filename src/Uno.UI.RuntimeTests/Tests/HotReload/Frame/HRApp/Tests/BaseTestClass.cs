using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

public class BaseTestClass
{
	[TestInitialize]
	public void InitHotReload()
	{
		// Reset the mapping store so tests start from a known state.
		// Pause/Resume on TypeMappings is obsolete (spec 041) — the new pause
		// mechanism (Uno.HotReload.Client.UIUpdate) is per-handle and never
		// retains state across tests.
		TypeMappings.ClearMappings();
	}
}
