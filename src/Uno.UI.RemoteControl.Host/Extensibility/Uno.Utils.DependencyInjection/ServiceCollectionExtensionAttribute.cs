using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Utils.DependencyInjection;

/// <summary>
/// Attribute to define a type able to registers some services in a service collection.
/// </summary>
/// <param name="type">Type of the extension that should be instantiated with a <see cref="IServiceCollection"/> as parameter.</param>
/// <remarks>
/// The given type is expected to have a single constructor which takes a single parameter of type <see cref="IServiceCollection"/>.
/// An instance of the given type will be created during the service collection registration process (with the service collection as parameter),
/// so the implementation will be able to add some custom services on it.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ServiceCollectionExtensionAttribute(Type type) : Attribute
{
	/// <summary>
	/// Type of the extension that should be instantiated with a <see cref="IServiceCollection"/> as parameter.
	/// </summary>
	public Type Type { get; } = type;
}
