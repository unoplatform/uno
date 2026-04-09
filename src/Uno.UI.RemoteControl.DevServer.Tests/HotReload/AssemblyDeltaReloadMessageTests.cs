using System.Collections.Immutable;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.DevServer.Tests.HotReload;

/// <summary>
/// Tests for the <see cref="AssemblyDeltaReload"/> message shape emitted by the server
/// for both the standard (non-debugger) path and the debugger path.
/// </summary>
/// <remarks>
/// The server emits different frame shapes depending on whether the app is running
/// with the mono debugger attached (VSCode extension with Uno Platform debugger):
/// <list type="bullet">
/// <item>
///   <term>Non-debugger path</term>
///   <description>
///     The server sends a single <see cref="AssemblyDeltaReload"/> frame that contains
///     all delta fields (MetadataDelta, ILDelta, PdbDelta, ModuleId, UpdatedTypes).
///     This frame passes <see cref="AssemblyDeltaReload.IsValid"/> on the client side.
///   </description>
/// </item>
/// <item>
///   <term>Debugger path</term>
///   <description>
///     The server sends the metadata, IL and PDB deltas to the IDE via
///     <see cref="HotReloadThruDebuggerIdeMessage"/>, and then sends a reduced
///     <see cref="AssemblyDeltaReload"/> frame containing only ModuleId and UpdatedTypes
///     (no delta bytes).  The client detects this case when running inside the VSCode
///     extension and applies only the updated-types information (the debugger handles
///     the actual metadata/IL application).
///   </description>
/// </item>
/// </list>
/// </remarks>
[TestClass]
public class AssemblyDeltaReloadMessageTests
{
	// Minimal base-64 placeholder values used by these tests; the actual content
	// does not matter because we are testing structure, not content.
	private const string SampleModuleId = "12345678-1234-1234-1234-123456789abc";
	private const string SampleBase64 = "AQID"; // base-64 for [1, 2, 3]
	private static readonly ImmutableHashSet<string> SampleFilePaths = ImmutableHashSet.Create("Sample.xaml");

	// ---------------------------------------------------------------------------
	// Non-debugger path
	// ---------------------------------------------------------------------------

	/// <summary>
	/// In the non-debugger path the server populates all delta fields so the client
	/// can apply the assembly update directly.
	/// </summary>
	[TestMethod]
	public void NonDebuggerPath_AssemblyDeltaReload_IsValid()
	{
		// Arrange – simulate what the server produces for the non-debugger path
		var message = new AssemblyDeltaReload
		{
			FilePaths = SampleFilePaths,
			ModuleId = SampleModuleId,
			MetadataDelta = SampleBase64,
			ILDelta = SampleBase64,
			PdbDelta = SampleBase64,
			UpdatedTypes = SampleBase64,
		};

		// Act & Assert
		message.IsValid().Should().BeTrue("the non-debugger path message must pass IsValid() so the client can apply the delta");
	}

	/// <summary>
	/// The non-debugger path frame must carry all four required delta fields.
	/// </summary>
	[TestMethod]
	public void NonDebuggerPath_AssemblyDeltaReload_ContainsAllDeltaFields()
	{
		// Arrange
		var message = new AssemblyDeltaReload
		{
			FilePaths = SampleFilePaths,
			ModuleId = SampleModuleId,
			MetadataDelta = SampleBase64,
			ILDelta = SampleBase64,
			PdbDelta = SampleBase64,
			UpdatedTypes = SampleBase64,
		};

		// Assert – every delta field must be present
		message.ModuleId.Should().NotBeNullOrEmpty("ModuleId is required to identify the assembly");
		message.MetadataDelta.Should().NotBeNullOrEmpty("MetadataDelta must be present in the non-debugger path");
		message.ILDelta.Should().NotBeNullOrEmpty("ILDelta must be present in the non-debugger path");
		message.PdbDelta.Should().NotBeNullOrEmpty("PdbDelta must be present in the non-debugger path");
		message.UpdatedTypes.Should().NotBeNullOrEmpty("UpdatedTypes must be present so XAML hot reload can refresh the correct types");
	}

	// ---------------------------------------------------------------------------
	// Debugger path – AssemblyDeltaReload (reduced frame sent to the app)
	// ---------------------------------------------------------------------------

	/// <summary>
	/// In the debugger path the server intentionally omits the delta bytes from the
	/// <see cref="AssemblyDeltaReload"/> frame because they are forwarded to the IDE.
	/// The frame therefore fails <see cref="AssemblyDeltaReload.IsValid"/> on its own.
	/// </summary>
	[TestMethod]
	public void DebuggerPath_ReducedAssemblyDeltaReload_IsNotValid()
	{
		// Arrange – simulate the reduced frame the server sends when the mono debugger is attached
		var message = new AssemblyDeltaReload
		{
			FilePaths = SampleFilePaths,
			ModuleId = SampleModuleId,
			UpdatedTypes = SampleBase64,
			// MetadataDelta, ILDelta and PdbDelta intentionally absent
		};

		// Act & Assert
		message.IsValid().Should().BeFalse("delta bytes are absent in the debugger path; the mono debugger applies them via the IDE channel");
	}

	/// <summary>
	/// Although the full <see cref="AssemblyDeltaReload.IsValid"/> check fails for the
	/// debugger path frame, the client can still process updated types when
	/// <c>_runningInsideVSCodeExtension</c> is true, using the relaxed check
	/// <c>UpdatedTypes is not null &amp;&amp; ModuleId is not null</c>.
	/// </summary>
	[TestMethod]
	public void DebuggerPath_ReducedAssemblyDeltaReload_HasRequiredFieldsForVSCodeExtension()
	{
		// Arrange – simulate the reduced frame the server sends when the mono debugger is attached
		var message = new AssemblyDeltaReload
		{
			FilePaths = SampleFilePaths,
			ModuleId = SampleModuleId,
			UpdatedTypes = SampleBase64,
			// MetadataDelta, ILDelta and PdbDelta intentionally absent
		};

		// Act – replicates the client-side check applied when _runningInsideVSCodeExtension is true
		// (see ClientHotReloadProcessor.Agent.cs)
		var validForVSCodeExtension = message.UpdatedTypes is not null && message.ModuleId is not null;

		// Assert
		validForVSCodeExtension.Should().BeTrue("the reduced frame must still carry ModuleId and UpdatedTypes so the client can refresh XAML after the debugger applies the deltas");
		message.MetadataDelta.Should().BeNull("delta bytes must NOT be in the reduced frame; they go via the IDE channel instead");
		message.ILDelta.Should().BeNull("delta bytes must NOT be in the reduced frame; they go via the IDE channel instead");
		message.PdbDelta.Should().BeNull("delta bytes must NOT be in the reduced frame; they go via the IDE channel instead");
	}

	// ---------------------------------------------------------------------------
	// Debugger path – HotReloadThruDebuggerIdeMessage (sent to the IDE)
	// ---------------------------------------------------------------------------

	/// <summary>
	/// The <see cref="HotReloadThruDebuggerIdeMessage"/> sent via the IDE channel in the
	/// debugger path must carry all three delta blobs and the module identifier.
	/// </summary>
	[TestMethod]
	public void DebuggerPath_IdeMessage_ContainsAllDeltaFields()
	{
		// Arrange – simulate what the server constructs for TrySendMessageToIDEAsync
		var ideMessage = new HotReloadThruDebuggerIdeMessage(
			ModuleId: SampleModuleId,
			MetadataDelta: SampleBase64,
			IlDelta: SampleBase64,
			PdbBytes: SampleBase64);

		// Assert
		ideMessage.ModuleId.Should().NotBeNullOrEmpty("the IDE needs the module GUID to target the correct assembly");
		ideMessage.MetadataDelta.Should().NotBeNullOrEmpty("the IDE/debugger needs MetadataDelta to apply the update");
		ideMessage.IlDelta.Should().NotBeNullOrEmpty("the IDE/debugger needs IlDelta to apply the update");
		ideMessage.PdbBytes.Should().NotBeNullOrEmpty("the IDE/debugger needs PdbBytes for symbol information");
	}
}
