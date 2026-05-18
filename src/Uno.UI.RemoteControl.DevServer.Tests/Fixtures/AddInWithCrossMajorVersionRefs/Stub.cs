using System;
using System.Text.Encodings.Web;
using Microsoft.Extensions.DependencyInjection;
using Uno.Utils.DependencyInjection;

// The host's Uno.Utils.DependencyInjection.ServiceCollectionServiceExtensions
// scans add-in assemblies for [ServiceCollectionExtension] by full type name
// and calls Activator.CreateInstance(extension.Type, args: [services]) on
// each registered type. Declaring this attribute makes the host actually
// instantiate ServicesRegistration during startup, so the v8.0.0.0
// AssemblyRefs compiled into this assembly (System.Text.Encodings.Web,
// Microsoft.Extensions.DependencyInjection.Abstractions) actually get
// resolved at runtime — exercising the AddInLoadContext load path and the
// AssemblyLoadContext.Default.Resolving handler in Uno.UI.RemoteControl.Host
// rather than sitting dormant in metadata.
[assembly: ServiceCollectionExtension(typeof(ServicesRegistration))]

namespace Uno.Utils.DependencyInjection;

/// <summary>
/// Activated by the host during add-in registration via the
/// <c>[ServiceCollectionExtension]</c> attribute above. The ctor's
/// <see cref="IServiceCollection"/> parameter and the
/// <see cref="JavaScriptEncoder.Default"/> touch both go through this
/// assembly's older-major <c>AssemblyRef</c>s, so the host's bridging in
/// <c>AddInLoadContext</c> must hand back its already-loaded newer-major
/// instances rather than letting the strict TPA binder throw
/// <see cref="System.IO.FileNotFoundException"/>.
/// <para>
/// Note: depending on the .NET runtime version, the binder may also
/// successfully lax-bind these refs even without the host's fix — the
/// surrounding test asserts the *positive* outcome (host stays up and the
/// add-in's ctor runs) rather than trying to deterministically reproduce
/// the original strict-bind FNF, which depends on runtime-version-specific
/// behaviour.
/// </para>
/// </summary>
public sealed class ServicesRegistration
{
	public ServicesRegistration(IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		// Force the runtime to resolve System.Text.Encodings.Web from this
		// assembly's compiled v8.0.0.0 AssemblyRef — mirrors how Kiota's
		// KiotaJsonSerializationContext..cctor triggers the original crash on
		// hosts that strict-match the TPA version.
		_ = JavaScriptEncoder.Default;
	}
}

// The host's ServiceCollectionExtensionAttribute lives in the same
// Uno.Utils.DependencyInjection namespace inside Uno.UI.RemoteControl.Host and
// is matched by full type name through CustomAttributesData. Add-ins declare
// their own copy under the same namespace+name so they don't have to take a
// direct dependency on the host assembly. (Same convention as the
// Uno.Settings.DevServer / Uno.UI.App.Mcp.Server add-ins ship today.)
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class ServiceCollectionExtensionAttribute(Type type) : Attribute
{
	public Type Type { get; } = type;
}
