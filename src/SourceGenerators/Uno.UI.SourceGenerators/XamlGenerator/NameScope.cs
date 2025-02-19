#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Uno.Extensions;

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
		public string Name => $"{Namespace.Replace(".", "")}{ClassName}";

		/// <summary>
		/// Name of the root class for all sub-classes
		/// </summary>
		/// <remarks>All sub-classes will be generated nested to this class, which is flagged with CreateNewOnMetadataUpdate attribute to avoid issues with HR.</remarks>
		public string SubClassesRoot => $"__{FileUniqueId}_{string.Join("_", [Name, .. Parents.Select(scope => scope.Name)])}";

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

		public List<string> ExplicitApplyMethods { get; } = [];

		/// <summary>
		/// Set of method builders to be generated for the current scope.
		/// This is usually used to avoid delegates like for event handlers.
		/// </summary>
		public List<Action<IIndentedStringBuilder>> CallbackMethods { get; } = [];

		public int ComponentCount => Components.Count;
	}
}
