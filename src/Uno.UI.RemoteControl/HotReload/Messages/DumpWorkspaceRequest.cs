using System;
using Newtonsoft.Json;

namespace Uno.UI.RemoteControl.HotReload.Messages;

/// <summary>
/// Request to pack the current workspace to a file.
/// </summary>
/// <param name="TargetFile">The target file where to save the workspace.</param>
public record PackWorkspaceRequest(string? TargetFile = null) : IMessage
{
	public const string Name = nameof(PackWorkspaceRequest);

	[JsonIgnore]
	public string Scope => WellKnownScopes.HotReload;
	[JsonIgnore]
	string IMessage.Name => Name;

	/// <summary>
	/// Gets or sets the identifier of the request.
	/// </summary>
	[JsonProperty]
	public string RequestId { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Represents the response returned after a workspace dump operation, containing the request identifier and the path to
/// the generated file.
/// </summary>
/// <param name="RequestId">The unique identifier associated with the workspace dump request.</param>
/// <param name="File">The file path to the dumped workspace data.</param>
/// <param name="Error">The error that prevented the workspace to be packed.</param>
public record PackWorkspaceResponse(string RequestId, string? File, string? Error) : IMessage
{
	public const string Name = nameof(PackWorkspaceResponse);
	[JsonIgnore]
	public string Scope => WellKnownScopes.HotReload;
	[JsonIgnore]
	string IMessage.Name => Name;
}


/// <summary>
/// Request to pack the current workspace to a file.
/// </summary>
/// <param name="PackageFile">The workspace package file to load.</param>
/// <param name="WorkingDir">The directory to use to extract and init workspace.</param>
public record LoadWorkspaceRequest(string PackageFile, string? WorkingDir = null) : IMessage
{
	public const string Name = nameof(LoadWorkspaceRequest);

	[JsonIgnore]
	public string Scope => WellKnownScopes.HotReload;
	[JsonIgnore]
	string IMessage.Name => Name;

	/// <summary>
	/// Gets or sets the identifier of the request.
	/// </summary>
	[JsonProperty]
	public string RequestId { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Represents the response returned after a workspace load operation, containing the request identifier and the path to
/// the generated file.
/// </summary>
/// <param name="RequestId">The unique identifier associated with the workspace dump request.</param>
/// <param name="Error">The error that prevented the workspace to be loaded or `null` if workspace loaded successfully.</param>
public record LoadWorkspaceResponse(string RequestId, string? Error) : IMessage
{
	public const string Name = nameof(LoadWorkspaceResponse);
	[JsonIgnore]
	public string Scope => WellKnownScopes.HotReload;
	[JsonIgnore]
	string IMessage.Name => Name;
}
