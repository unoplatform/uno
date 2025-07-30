#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Uno.Extensions;
using Uno.UI.SourceGenerators.XamlGenerator.Utils;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	/// <summary>
	/// Represents the current scope of the XAML generation
	/// </summary>
	/// <param name="Parents">The stack of parent scopes</param>
	/// <param name="FileUniqueId">Unique ID of the generated file.</param>
	/// <param name="Namespace"></param>
	/// <param name="ClassName"></param>
	internal record NameScope(ImmutableStack<NameScope> Parents, string FileUniqueId, string Namespace, string ClassName)
	{
		private readonly Dictionary<string, Action<IIndentedStringBuilder>> _methods = new();

		public string Name => $"{Namespace.Replace(".", "")}{ClassName}";

		/// <summary>
		/// Name of the root class for all sub-classes
		/// </summary>
		/// <remarks>All sub-classes will be generated nested to this class, which is flagged with CreateNewOnMetadataUpdate attribute to avoid issues with HR.</remarks>
		public string SubClassesRoot => $"__{FileUniqueId}"; // Note: The SubClassesRoot is no longer relative to the current scope!

		/// <summary>
		/// Used to detect duplicate x:Name and report error for those.
		/// </summary>
		public HashSet<string> DeclaredNames { get; } = [];

		public List<BackingFieldDefinition> BackingFields { get; } = [];

		/// <summary>
		/// List of action handlers for registering x:Bind events
		/// </summary>
		public List<XBindEventInitializerDefinition> xBindEventsHandlers { get; } = [];

		/// <summary>
		/// Lists the ElementStub builder holder variables used to pin references for implicit pinning platforms
		/// </summary>
		public List<string> ElementStubHolders { get; } = [];

		public HashSet<string> ReferencedElementNames { get; } = [];

		public Dictionary<string, Subclass> Subclasses { get; } = [];

		public List<Action<IIndentedStringBuilder>> SubclassBuilders { get; } = [];

		public List<ComponentDefinition> Components { get; } = [];

		public List<XamlObjectDefinition> XBindExpressions { get; } = [];

		public List<string> XBindTryGetMethodDeclarations { get; } = [];

		public int ComponentCount => Components.Count;

		/// <summary>
		/// Set of method builders to be generated for the current scope.
		/// This is usually used to avoid delegates like for event handlers.
		/// </summary>
		public IImmutableList<Action<IIndentedStringBuilder>> Methods => _methods.Values.ToImmutableList();

		/// <summary>
		/// Registers a method to be generated in the current scope.
		/// </summary>
		/// <param name="name">The suggested method name.</param>
		/// <param name="methodBuilder">Method builder accepting the effective name of the method as parameter.</param>
		/// <returns>The effective method name.</returns>
		public string RegisterMethod(string name, Action<string, IIndentedStringBuilder> methodBuilder)
		{
			return name = NamingHelper.AddUnique(_methods, name, BuildMethod);

			void BuildMethod(IIndentedStringBuilder builder)
				=> methodBuilder(name, builder);
		}
	}
}
