using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.DependencyInjection;

[AttributeUsage(AttributeTargets.Assembly)]
public class ServiceAttribute(Type contract, Type implementation) : Attribute
{
	public ServiceAttribute(Type implementation)
		: this(implementation, implementation)
	{
	}

	public Type Contract { get; } = contract;

	public Type Implementation { get; } = implementation;

	public ServiceLifetime LifeTime { get; set; } = ServiceLifetime.Singleton;

	public bool IsAutoInit { get; set; }
}
