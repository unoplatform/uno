using System;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.UI.RemoteControl.Messages;

public record ProcessorsDiscoveryResponse(IImmutableList<string> Assemblies, IImmutableList<DiscoveredProcessor> Processors) : IMessage
{
	public const string Name = nameof(ProcessorsDiscoveryResponse);

	public string Scope => WellKnownScopes.DevServerChannel;

	string IMessage.Name => Name;
}

public record DiscoveredProcessor(string AssemblyPath, string Type, string Version, bool IsLoaded, string? LoadError = null);
