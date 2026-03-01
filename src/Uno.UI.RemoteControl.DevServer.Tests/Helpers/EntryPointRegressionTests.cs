using System.IO;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

/// <summary>
/// Regression tests that verify the VS extension reflection-probed types and constructor signatures
/// in EntryPoint.cs remain stable. The VS extension (DevServerLauncher) uses reflection to probe
/// these type names and constructor signatures. Any change would break the extension.
/// </summary>
[TestClass]
public class EntryPointRegressionTests
{
	private static string GetEntryPointSourcePath()
	{
		// Navigate from test assembly output to the VS project source
		var testDir = Path.GetDirectoryName(typeof(EntryPointRegressionTests).Assembly.Location)!;
		var srcRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));
		return Path.Combine(srcRoot, "Uno.UI.RemoteControl.VS", "EntryPoint.cs");
	}

	private static string ReadEntryPointSource()
	{
		var path = GetEntryPointSourcePath();
		if (!File.Exists(path))
		{
			Assert.Inconclusive($"EntryPoint.cs not found at expected path: {path}. This test requires the source tree.");
		}

		return File.ReadAllText(path);
	}

	[TestMethod]
	public void EntryPoint_Namespace_IsCorrect()
	{
		var source = ReadEntryPointSource();
		source.Should().Contain("namespace Uno.UI.RemoteControl.VS;");
	}

	[TestMethod]
	public void EntryPoint_ClassName_IsPublicPartial()
	{
		var source = ReadEntryPointSource();
		source.Should().Contain("public partial class EntryPoint");
	}

	[TestMethod]
	public void EntryPoint_V2Constructor_HasGlobalPropertiesProvider()
	{
		var source = ReadEntryPointSource();
		// V2 constructor signature: (DTE2, string, AsyncPackage, Action<Func<Task<Dictionary<string, string>>>>, Func<Task>)
		source.Should().Contain("Action<Func<Task<Dictionary<string, string>>>> globalPropertiesProvider");
	}

	[TestMethod]
	public void EntryPoint_V3Constructor_HasVsixChannelHandle()
	{
		var source = ReadEntryPointSource();
		// V3 constructor signature: (DTE2, string, AsyncPackage, string vsixChannelHandle)
		source.Should().Contain("string vsixChannelHandle)");
	}
}
