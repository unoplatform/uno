using System.Collections.Immutable;
using Uno.UI.RemoteControl.Helpers;

namespace Uno.UI.RemoteControl.Host.Extensibility;

public record AddInsStatus(AddInsDiscoveryResult Discovery, IImmutableList<AssemblyLoadResult> Assemblies)
{
	public static AddInsStatus Empty { get; } = new(AddInsDiscoveryResult.Empty(), ImmutableArray<AssemblyLoadResult>.Empty);
}
