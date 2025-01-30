#nullable enable

using System;
using System.Collections.Generic;
using Uno.Extensions;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class NameScope
	{
		public NameScope(string? @namespace, string className)
		{
			Namespace = @namespace ?? string.Empty;
			ClassName = className;
		}

		public string Name => $"{Namespace.Replace(".", "")}{ClassName}";
		public string Namespace { get; private set; }
		public string ClassName { get; private set; }

		/// <summary>
		/// Used to detect duplicate x:Name and report error for those.
		/// </summary>
		public HashSet<string> DeclaredNames { get; } = [];

		public List<BackingFieldDefinition> BackingFields { get; } = [];

		/// <summary>
		/// List of action handlers for registering x:Bind events
		/// </summary>
		public List<EventHandlerBackingFieldDefinition> xBindEventsHandlers { get; } = [];

		/// <summary>
		/// Lists the ElementStub builder holder variables used to pin references for implicit pinning platforms
		/// </summary>
		public List<string> ElementStubHolders { get; } = [];

		public HashSet<string> ReferencedElementNames { get; } = [];

		public Dictionary<string, Subclass> Subclasses { get; } = [];

		public List<ComponentDefinition> Components { get; } = [];

		public List<XamlObjectDefinition> XBindExpressions { get; } = [];

		public List<string> XBindTryGetMethodDeclarations { get; } = [];

		public List<string> ExplicitApplyMethods { get; } = [];

		public List<Action<IIndentedStringBuilder>> EventHandlers { get; } = [];

		public int ComponentCount => Components.Count;
	}
}
