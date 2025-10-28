#nullable enable
using System;
using System.Linq;

namespace Uno.UI.RemoteControl;

public static class WellKnownScopes
{
	/// <summary>
	/// Reserved for internal usage of communication channel between dev-server and IDE (i.e. KeepAliveIdeMessage).
	/// </summary>
	public const string IdeChannel = "IdeChannel";

	/// <summary>
	/// Reserved for internal usage of communication channel between dev-server and client (i.e. KeepAliveMessage).
	/// </summary>
	public const string DevServerChannel = "RemoteControlServer";

	/// <summary>
	/// For hot-reload messages, client, server and IDE.
	/// </summary>
	public const string HotReload = "HotReload";

	/// <summary>
	/// For hot-reload messages, client, server and IDE.
	/// </summary>
	public const string HotAssets = "HotAssets";

	/// <summary>
	/// For messages used for testing purpose (e.g. UpdateFile)
	/// </summary>
	public const string Testing = "UnoRuntimeTests";

	/// <summary>
	/// For generic messages initiated by the IDE (like a command)
	/// </summary>
	public const string Ide = "IDE";
}
