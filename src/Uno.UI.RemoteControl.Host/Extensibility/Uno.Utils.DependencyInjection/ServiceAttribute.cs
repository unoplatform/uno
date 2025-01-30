using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Utils.DependencyInjection;

/// <summary>
/// Attribute to define a service registration in the service collection.
/// </summary>
/// <param name="contract">Type of the contract (i.e. interface) implemented by the concrete <see cref="Implementation"/> type.</param>
/// <param name="implementation">Concrete type to register in the service collection.</param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ServiceAttribute(Type contract, Type implementation) : Attribute
{
	/// <summary>
	/// Creates a new instance of the <see cref="ServiceAttribute"/> class with only a concrete type (used as contract and implementation).
	/// </summary>
	/// <param name="implementation">Concrete type to register in the service collection.</param>
	public ServiceAttribute(Type implementation)
		: this(implementation, implementation)
	{
	}

	/// <summary>
	/// Type of the contract (i.e. interface) implemented by the concrete <see cref="Implementation"/> type.
	/// </summary>
	public Type Contract { get; } = contract;

	/// <summary>
	/// Concrete type to register in the service collection.
	/// </summary>
	public Type Implementation { get; } = implementation;

	/// <summary>
	/// The lifetime of the service.
	/// </summary>
	public ServiceLifetime LifeTime { get; set; } = ServiceLifetime.Singleton;

	/// <summary>
	/// Indicates if the service should be automatically initialized at startup (a.k.a. hosted service).
	/// </summary>
	public bool IsAutoInit { get; set; }

	/// <summary>
	/// Provides a key to identify the service (service will be registered as keyed service if not null).
	/// </summary>
	public object? Key { get; set; }
}
