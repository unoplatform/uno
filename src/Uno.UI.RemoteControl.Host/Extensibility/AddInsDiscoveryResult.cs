using System.Collections.Immutable;

namespace Uno.UI.RemoteControl.Host.Extensibility;

public record AddInsDiscoveryResult(string? Error, ImmutableList<string> AddIns)
{
	public static AddInsDiscoveryResult Success(ImmutableList<string> assemblies) => new(null, assemblies);
	public static AddInsDiscoveryResult Failed(string error) => new(error, ImmutableList<string>.Empty);
	public static AddInsDiscoveryResult Empty() => new(null, ImmutableList<string>.Empty);
}
