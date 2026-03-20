using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Uno.Extensions;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	internal class HotReloadWorkspaceLoadResult : IMessage
	{
		public const string Name = "UpdateFile"; // This is intentional to keep original error to stay backward compatible.

		[JsonProperty]
		public bool WorkspaceInitialized { get; set; }

		[JsonIgnore]
		public string Scope => WellKnownScopes.HotReload;

		[JsonIgnore]
		string IMessage.Name => Name;
	}
}
