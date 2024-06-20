using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Uno.Extensions;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	/// <summary>
	/// Message sent by the dev-server when it detects a file change in the solution.
	/// </summary>
	/// <remarks>This is being sent only for xaml and cs files.</remarks>
	internal class FileReload : IMessage // a.k.a. FileUpdated
	{
		public const string Name = nameof(FileReload);

		[JsonProperty]
		public string? FilePath { get; set; }

		[JsonProperty]
		public string? Content { get; set; }

		[JsonIgnore]
		public string Scope => WellKnownScopes.HotReload;

		[JsonIgnore]
		string IMessage.Name => Name;

		[MemberNotNullWhen(true, nameof(FilePath), nameof(Content))]
		public bool IsValid()
			=> !FilePath.IsNullOrEmpty() && Content is not null;
	}
}
