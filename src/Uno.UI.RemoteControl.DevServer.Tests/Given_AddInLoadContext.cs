using System.Reflection;
using System.Runtime.Loader;
using Uno.UI.RemoteControl.Host.Helpers;

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
	[Description("Even when a dynamic assembly is created with the same simple name as a real assembly in the same process, the bridge must return the static assembly and never the dynamic one.")]
	public void TryBridgeBySimpleName_SkipsDynamicAssemblies_WhenCandidateIsDynamic()
	{
		var staticAsm = typeof(System.Text.Json.JsonDocument).Assembly;

		// Create a dynamic assembly inside the Default ALC with the same simple
		// name as the static target. AssemblyBuilder.DefineDynamicAssembly with
		// AssemblyBuilderAccess.Run registers the dynamic assembly with the
		// currently-executing ALC (Default for test code). The IsDynamic guard
		// in TryBridgeBySimpleName must skip it so that the static assembly is
		// returned, not the dynamic one.
		_ = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(
			new AssemblyName(staticAsm.GetName().Name!),
			System.Reflection.Emit.AssemblyBuilderAccess.Run);

		var requested = new AssemblyName("System.Text.Json");
		requested.SetPublicKeyToken(staticAsm.GetName().GetPublicKeyToken());

		var result = HostAssemblyResolution.TryBridgeBySimpleName(AssemblyLoadContext.Default, requested);

		result.Should().NotBeNull("the static assembly is present in Default.Assemblies");
		result!.IsDynamic.Should().BeFalse(
			"the bridge must never return a dynamic assembly even when one with a matching simple name exists");
		result.Should().BeSameAs(staticAsm,
			"the bridge must return the static assembly when a same-named dynamic one is present");
	}

	// ------------------------------------------------------------------ step 1 bridges without engaging Default.Resolving

	[TestMethod]
	[Description("Step 1 must satisfy a host-loaded AssemblyRef directly from Default.Assemblies without delegating to AssemblyLoadContext.Default.Resolving (proving the bridge short-circuits before the fallback).")]
	public void Load_Step1_BridgesHostAssembly_WithoutGoingThroughDefaultResolving()
	{
		// Force the assembly into Default.Assemblies (well-known host assembly).
		var hostAssembly = typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly;

		var resolvingCount = 0;
		ResolveEventHandlerNoop counter = (ctx, req) =>
		{
			Interlocked.Increment(ref resolvingCount);
			return null;
		};
		AssemblyLoadContext.Default.Resolving += counter.Invoke;
		try
		{
			var ctx = new AddInLoadContext(Array.Empty<string>());
			var loaded = ctx.LoadFromAssemblyName(new AssemblyName("Microsoft.Extensions.DependencyInjection.Abstractions"));

			loaded.Should().BeSameAs(hostAssembly,
				"step 1 must bridge to the host's already-loaded instance");
			resolvingCount.Should().Be(0,
				"step 1 must satisfy the request without triggering Default.Resolving");
		}
		finally
		{
			AssemblyLoadContext.Default.Resolving -= counter.Invoke;
		}
	}

	private delegate Assembly? ResolveEventHandlerNoop(AssemblyLoadContext context, AssemblyName name);

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
