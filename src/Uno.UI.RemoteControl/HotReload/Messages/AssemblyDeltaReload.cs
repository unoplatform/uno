using Newtonsoft.Json;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	internal class AssemblyDeltaReload : IMessage
	{
		public const string Name = nameof(AssemblyDeltaReload);

		[JsonProperty]
		public string FilePath { get; set; }

		[JsonProperty]
		public string ModuleId { get; set; }

		[JsonProperty]
		public string ILDelta { get; set; }

		[JsonProperty]
		public string MetadataDelta { get; set; }

		[JsonProperty]
		public string PdbDelta { get; set; }

		[JsonProperty]
		public string UpdatedTypes { get; set; }

		[JsonIgnore]
		public string Scope => HotReloadConstants.ScopeName;

		[JsonIgnore]
		string IMessage.Name => Name;
	}
}
