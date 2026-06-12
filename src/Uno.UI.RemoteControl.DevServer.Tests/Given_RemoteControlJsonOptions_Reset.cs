using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RemoteControl.Messaging;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public class Given_RemoteControlJsonOptions_Reset
{
	[TestCleanup]
	public void Cleanup() => RemoteControlJsonOptions.Reset();

	[TestMethod]
	public void When_Reset_Then_Collectible_Registered_Context_Is_Released()
	{
		var tracker = RegisterCollectibleContextAndDropStrongReference(out var weakAlc);

		RemoteControlJsonOptions.Reset();

		for (var i = 0; i < 10 && (tracker.IsAlive || weakAlc.IsAlive); i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		Assert.IsFalse(
			tracker.IsAlive,
			"Reset() must drop a COLLECTIBLE registered JsonSerializerContext. A secondary " +
			"(collectible-ALC) app registers a per-ALC context; holding it on this shared static " +
			"pins that app's AssemblyLoadContext after unload.");
	}

	[TestMethod]
	public void When_Reset_Then_Host_Context_Follows_Target_Contract()
	{
		var context = new ResetProbeJsonContext(new JsonSerializerOptions());
		RemoteControlJsonOptions.SetSourceGeneratedContext(context);

		RemoteControlJsonOptions.Reset();

		var resolver = RemoteControlJsonOptions.Default.TypeInfoResolver;

		// The net5+ build preserves a host (non-collectible) context so surviving clients keep
		// source-generated serialization; the netstandard2.0 build cannot test collectibility
		// and documents dropping the context unconditionally.
		var frameworkName = typeof(RemoteControlJsonOptions).Assembly
			.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName;
		if (frameworkName?.StartsWith(".NETStandard", StringComparison.Ordinal) == true)
		{
			Assert.IsInstanceOfType(
				resolver,
				typeof(DefaultJsonTypeInfoResolver),
				"The netstandard2.0 fallback drops the registered context on Reset().");
		}
		else
		{
			Assert.IsNotInstanceOfType(
				resolver,
				typeof(DefaultJsonTypeInfoResolver),
				"After Reset(), the lazily recreated options must still combine the preserved host " +
				"source-generated context rather than fall back to reflection-only resolution.");
		}
	}

	[TestMethod]
	public void When_Reset_Then_Default_Options_Are_Recreated_Lazily()
	{
		RemoteControlJsonOptions.Reset();

		var options = RemoteControlJsonOptions.Default;

		Assert.IsNotNull(options, "Default options must be recreated lazily after Reset().");
	}

	// Registering then dropping the only strong reference in a non-inlined frame ensures the
	// just-registered context is not pinned by a lingering local when we assert collection.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference RegisterCollectibleContextAndDropStrongReference(out WeakReference weakAlc)
	{
		// Load a second copy of this test assembly into a real collectible ALC and instantiate
		// the probe context from that copy — the same collectibility shape as a secondary app's
		// per-ALC RemoteControlJsonContext (JsonSerializerContext resolves to the shared copy).
		var alc = new AssemblyLoadContext("JsonOptionsResetProbe", isCollectible: true);
		var copy = alc.LoadFromAssemblyPath(typeof(ResetProbeJsonContext).Assembly.Location);
		var contextType = copy.GetType(typeof(ResetProbeJsonContext).FullName!)!;
		var context = (JsonSerializerContext)Activator.CreateInstance(contextType, new JsonSerializerOptions())!;

		Assert.IsTrue(context.GetType().IsCollectible, "Pre-condition: the probe context must be collectible");

		RemoteControlJsonOptions.SetSourceGeneratedContext(context);

		alc.Unload();
		weakAlc = new WeakReference(alc);
		return new WeakReference(context);
	}
}

[JsonSerializable(typeof(string))]
internal partial class ResetProbeJsonContext : JsonSerializerContext
{
}
