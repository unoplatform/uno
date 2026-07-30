using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Uno.UI.RemoteControl.Host;
using Uno.UI.RemoteControl.Messages;

namespace Uno.UI.RemoteControl.ServerCore;

/// <summary>
/// Represents the outcome of a processor discovery request.
/// </summary>
public sealed class RemoteControlProcessorDiscoveryResult
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RemoteControlProcessorDiscoveryResult"/> class.
	/// </summary>
	public RemoteControlProcessorDiscoveryResult(
		IImmutableList<string> assemblies,
		IImmutableList<DiscoveredProcessor> processors,
		IReadOnlyList<IServerProcessor> instances,
		IRemoteControlProcessorLease? lease = null)
	{
		Assemblies = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
		Processors = processors ?? throw new ArgumentNullException(nameof(processors));
		Instances = instances ?? throw new ArgumentNullException(nameof(instances));
		Lease = lease;
	}

	/// <summary>
	/// Gets the assembly file paths that were inspected during discovery.
	/// </summary>
	public IImmutableList<string> Assemblies { get; }

	/// <summary>
	/// Gets the processor metadata reported back to clients.
	/// </summary>
	public IImmutableList<DiscoveredProcessor> Processors { get; }

	/// <summary>
	/// Gets the instantiated server processors ready to be registered.
	/// </summary>
	public IReadOnlyList<IServerProcessor> Instances { get; }

	/// <summary>
	/// Gets an optional lease controlling native resources (e.g., assembly load contexts) linked to this discovery.
	/// </summary>
	public IRemoteControlProcessorLease? Lease { get; }
}