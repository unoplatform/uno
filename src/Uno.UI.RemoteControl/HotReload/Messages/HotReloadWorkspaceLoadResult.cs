using System.Text.Json.Serialization;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	public class HotReloadWorkspaceLoadResult : IMessage
	{
		public const string Name = "UpdateFile"; // This is intentional to keep original error to stay backward compatible.

		public bool WorkspaceInitialized { get; set; }

		[JsonIgnore]
		public string Scope => WellKnownScopes.HotReload;

		[JsonIgnore]
		string IMessage.Name => Name;
	}
}
