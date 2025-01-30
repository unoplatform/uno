using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Uno.Extensions;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	internal class HotReloadWorkspaceLoadResult : IMessage
	{
		public const string Name = nameof(UpdateFile);

		[JsonProperty]
		public bool WorkspaceInitialized { get; set; }

		[JsonIgnore]
		public string Scope => WellKnownScopes.HotReload;

		[JsonIgnore]
		string IMessage.Name => Name;
	}
}
