using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RemoteControl.Messaging;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public class Given_RemoteControlJsonOptions_Reset
{
	[TestCleanup]
	public void Cleanup() => RemoteControlJsonOptions.Reset();

	[TestMethod]
	public void When_Reset_Then_Registered_SourceGenerated_Context_Is_Released()
	{
		var tracker = RegisterContextAndDropStrongReference();

		RemoteControlJsonOptions.Reset();

		for (var i = 0; i < 10 && tracker.IsAlive; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		Assert.IsFalse(
			tracker.IsAlive,
			"Reset() must drop the cached options and the registered JsonSerializerContext. A secondary " +
			"(collectible-ALC) app registers a per-ALC context; holding it on this shared static pins that " +
			"app's AssemblyLoadContext after unload.");
	}

	[TestMethod]
	public void When_Reset_Then_Default_Options_Are_Recreated_Lazily()
	{
		RemoteControlJsonOptions.Reset();

		var options = RemoteControlJsonOptions.Default;

		Assert.IsNotNull(options, "Default options must be recreated lazily after Reset().");
	}

	// Registering then dropping the only strong reference in a non-inlined frame ensures the just-registered
	// context is not pinned by a lingering local when we assert collection.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference RegisterContextAndDropStrongReference()
	{
		var context = new ResetProbeJsonContext(new JsonSerializerOptions());
		RemoteControlJsonOptions.SetSourceGeneratedContext(context);
		return new WeakReference(context);
	}
}

[JsonSerializable(typeof(string))]
internal partial class ResetProbeJsonContext : JsonSerializerContext
{
}
