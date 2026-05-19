using System.Reflection;
using System.Runtime.Loader;
using Uno.UI.RemoteControl.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests;

/// <summary>
/// Unit tests for <see cref="AddInLoadContext"/> assembly-bridging behaviour.
/// These tests verify that Load() returns the host's already-loaded Assembly for
/// framework and contract assemblies (step 1 of the three-step resolution chain),
/// preserving Type identity across the host/add-in ALC boundary.
/// </summary>
[TestClass]
public class Given_AddInLoadContext
{
	// ------------------------------------------------------------------ step 1: bridge via Default.Assemblies

	[TestMethod]
	[Description("Step 1 must return the host's System.Text.Json instance so JsonDocument/JsonElement types are identical between host and add-in.")]
	public void Load_ResolvesFrameworkOobAssembly_ToHostInstance()
	{
		// Force the assembly into Default.Assemblies (reference a type from it first).
		var hostAssembly = typeof(System.Text.Json.JsonDocument).Assembly;

		var ctx = new AddInLoadContext(Array.Empty<string>());
		var loaded = ctx.LoadFromAssemblyName(new AssemblyName("System.Text.Json"));

		loaded.Should().BeSameAs(hostAssembly,
			"System.Text.Json must bridge to the host's already-loaded instance");
	}

	[TestMethod]
	[Description("Step 1 must return the host's Logging.Abstractions so ILogger contracts are identical between host and add-in.")]
	public void Load_ResolvesLoggingAbstractions_ToHostInstance()
	{
		var hostAssembly = typeof(ILogger).Assembly;

		var ctx = new AddInLoadContext(Array.Empty<string>());
		var loaded = ctx.LoadFromAssemblyName(new AssemblyName("Microsoft.Extensions.Logging.Abstractions"));

		loaded.Should().BeSameAs(hostAssembly,
			"ILogger assembly must bridge to the host's instance for log sinks to work across the boundary");
	}

	[TestMethod]
	[Description("Step 1 must return the host's System.Text.Encodings.Web — the assembly whose version mismatch triggered the original crash (PR #23287).")]
	public void Load_ResolvesEncodingsWeb_ToHostInstance()
	{
		// Touching JavaScriptEncoder forces the assembly into Default.Assemblies.
		var hostAssembly = typeof(System.Text.Encodings.Web.JavaScriptEncoder).Assembly;

		var ctx = new AddInLoadContext(Array.Empty<string>());
		var loaded = ctx.LoadFromAssemblyName(new AssemblyName("System.Text.Encodings.Web"));

		loaded.Should().BeSameAs(hostAssembly,
			"System.Text.Encodings.Web must bridge to the host's instance regardless of which major version the add-in compiled against");
	}

	// ------------------------------------------------------------------ IsDynamic guard

	[TestMethod]
	[Description("Dynamic (reflection-emit) assemblies must not be returned by the bridge, even if their auto-generated name coincidentally matches an AssemblyRef.")]
	public void Load_SkipsDynamicAssemblies_WhenBridging()
	{
		// Create a dynamic assembly whose simple name matches a real one.
		var dynamicName = "System.Text.Json";
		var dynamicAsm = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(
			new AssemblyName(dynamicName),
			System.Reflection.Emit.AssemblyBuilderAccess.Run);

		dynamicAsm.IsDynamic.Should().BeTrue("sanity check on our test setup");

		// The actual System.Text.Json must be in Default.Assemblies.
		var hostAssembly = typeof(System.Text.Json.JsonDocument).Assembly;

		var ctx = new AddInLoadContext(Array.Empty<string>());

		// The bridge must return the real assembly, not the dynamic one, despite the name match.
		// (The dynamic assembly isn't in Default.Assemblies unless we loaded it, but Load
		// iterates Default.Assemblies and the IsDynamic guard must skip it if it were there.)
		var loaded = ctx.LoadFromAssemblyName(new AssemblyName("System.Text.Json"));

		loaded.Should().BeSameAs(hostAssembly, "dynamic assemblies must never be returned by the bridge");
		loaded!.IsDynamic.Should().BeFalse("returned assembly must not be a dynamic assembly");
	}

	// ------------------------------------------------------------------ null / miss

	[TestMethod]
	[Description("Load returns null for an assembly not in Default.Assemblies, not in TPA, and not in any resolver — the ALC then throws FileNotFoundException as specified.")]
	public void LoadFromAssemblyName_ThrowsFileNotFoundException_WhenAssemblyIsUnknown()
	{
		var ctx = new AddInLoadContext(Array.Empty<string>());

		// Totally unknown assembly: Load returns null → ALC throws FNFE.
		var act = () => ctx.LoadFromAssemblyName(new AssemblyName("Totally.Unknown.Assembly.XYZ.DoesNotExist"));

		act.Should().Throw<FileNotFoundException>(
			"when Load returns null for all contexts, the runtime raises FileNotFoundException");
	}
}
