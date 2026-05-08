#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Uno.UI.Helpers
{
	/// <summary>
	/// Returns the set of assemblies that should be searched when resolving a type by name.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When <see cref="AssemblyLoadContext.CurrentContextualReflectionContext"/> is set to a
	/// non-default ALC (i.e. a caller is inside an
	/// <see cref="AssemblyLoadContext.EnterContextualReflection(System.Reflection.Assembly?)"/>
	/// scope rooted in a non-default ALC), the contextual ALC's
	/// <see cref="AssemblyLoadContext.Assemblies"/> are returned, followed by the default ALC's
	/// assemblies. This avoids picking up stale or sibling per-app ALCs that
	/// <see cref="AppDomain.GetAssemblies"/> would otherwise expose long after they have been
	/// <see cref="AssemblyLoadContext.Unload"/>ed but not yet finalised.
	/// </para>
	/// <para>
	/// When no contextual scope is set, or the scope is the default ALC, the behaviour is
	/// identical to <see cref="AppDomain.GetAssemblies"/> — there is no consumer-visible change.
	/// </para>
	/// <para>
	/// <strong>Entry points that trigger assembly resolution</strong><br/>
	/// The following public Uno Platform APIs ultimately call
	/// <see cref="GetRelevantAssemblies"/> and therefore benefit from an active
	/// <see cref="AssemblyLoadContext.EnterContextualReflection"/> scope when user types live
	/// in a non-default ALC:
	/// <list type="bullet">
	///   <item>
	///     <description>
	///       <see cref="Microsoft.UI.Xaml.Markup.XamlReader.Load(string)"/> and
	///       <see cref="Microsoft.UI.Xaml.Markup.XamlReader.LoadWithInitialTemplateValidation(string)"/> —
	///       these parse a XAML string at run time and need to resolve type names (e.g.
	///       <c>x:Class</c>, element type names, converter types) against the correct ALC.
	///       The call chain is:
	///       <c>XamlReader.Load → XamlObjectBuilder.Build → XamlTypeResolver.SourceFindType → GetRelevantAssemblies</c>.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	///       <see cref="Microsoft.UI.Xaml.DependencyObject.SetBinding(Microsoft.UI.Xaml.DependencyProperty, Microsoft.UI.Xaml.Data.BindingBase)"/>
	///       and the string-based overload
	///       <c>DependencyObject.SetBinding(string, BindingBase)</c> —
	///       when the property path contains an attached-property segment such as
	///       <c>(MyNamespace:MyType.MyProperty)</c>, the type name is resolved via
	///       <c>DependencyProperty.GetProperty → DependencyPropertyDescriptor.Parse → SearchTypeInLoadedAssemblies → GetRelevantAssemblies</c>.
	///     </description>
	///   </item>
	/// </list>
	/// </para>
	/// <para>
	/// <strong>How to use <see cref="AssemblyLoadContext.EnterContextualReflection"/></strong><br/>
	/// If your application loads assemblies into a non-default
	/// <see cref="AssemblyLoadContext"/> (for example as part of a plug-in or hot-reload
	/// scenario) and then calls any of the entry points listed above, wrap the call in a
	/// contextual-reflection scope so that Uno resolves types from the correct ALC:
	/// <code lang="csharp">
	/// // alc is the AssemblyLoadContext that contains the user types.
	/// // assemblyInContext is any assembly already loaded into that ALC.
	/// using (alc.EnterContextualReflection())
	/// {
	///     // XamlReader.Load will resolve type names against 'alc' first,
	///     // then fall back to the default ALC.
	///     var element = XamlReader.Load(xamlString);
	/// }
	///
	/// // Bindings with cross-ALC attached-property paths:
	/// using (alc.EnterContextualReflection())
	/// {
	///     myElement.SetBinding("(local:MyAttached.Value)", new Binding { Path = new PropertyPath("Value") });
	/// }
	/// </code>
	/// When the scope is not set (or the default ALC is active), the resolver falls back to
	/// <see cref="AppDomain.GetAssemblies"/> and behaviour is unchanged.
	/// </para>
	/// </remarks>
	internal static class ContextualAssemblyResolver
	{
		/// <summary>
		/// Returns the assemblies relevant for type-by-name resolution at the current
		/// reflection context.
		/// </summary>
		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "AppDomain.GetAssemblies is the documented fallback")]
		public static IEnumerable<Assembly> GetRelevantAssemblies()
		{
			var contextual = AssemblyLoadContext.CurrentContextualReflectionContext;
			if (contextual is not null && contextual != AssemblyLoadContext.Default)
			{
				return contextual.Assemblies.Concat(AssemblyLoadContext.Default.Assemblies);
			}

			return AppDomain.CurrentDomain.GetAssemblies();
		}
	}
}
