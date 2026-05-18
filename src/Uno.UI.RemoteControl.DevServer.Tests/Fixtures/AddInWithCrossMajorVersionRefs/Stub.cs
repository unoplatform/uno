using System.Text.Encodings.Web;

namespace Uno.UI.RemoteControl.DevServer.Tests.Fixtures.AddInWithCrossMajorVersionRefs;

/// <summary>
/// Stub type for the DevServer add-in load regression test fixture.
/// Its static cctor touches <see cref="JavaScriptEncoder.Default"/> so that the
/// compiled assembly carries an <c>AssemblyRef</c> to
/// <c>System.Text.Encodings.Web</c> at the version matching this project's
/// <c>TargetFramework</c> (net8.0 → v8.0.0.0). When the host loads this DLL as
/// an add-in, that AssemblyRef must be satisfied by the host's already-loaded
/// newer-major instance — exercising the bridging in
/// <c>Uno.UI.RemoteControl.Host.Helpers.AddInLoadContext</c> and the
/// <c>AssemblyLoadContext.Default.Resolving</c> handler.
/// </summary>
public static class Stub
{
	static Stub()
	{
		// Force the runtime to resolve System.Text.Encodings.Web during type
		// initialization, mirroring how Microsoft.Kiota.Serialization.Json's
		// KiotaJsonSerializationContext..cctor triggers the original crash.
		_ = JavaScriptEncoder.Default;
	}

	public static string GetEncoderName() => JavaScriptEncoder.Default.GetType().FullName!;
}
