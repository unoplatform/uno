#nullable enable

using System.Collections.Generic;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class NameScope
	{
		public NameScope(string? @namespace, string className, IXamlLocation? location)
		{
			Namespace = @namespace ?? string.Empty;
			ClassName = className;
			Location = location;
		}

		public string Name => $"{Namespace.Replace(".", "")}{ClassName}";
		public string Namespace { get; private set; }
		public string ClassName { get; private set; }
		public IXamlLocation? Location { get; }

		/// <summary>
		/// Used to detect duplicate x:Name and report error for those.
		/// </summary>
		public HashSet<string> DeclaredNames { get; } = new HashSet<string>();

		public List<BackingFieldDefinition> BackingFields { get; } = new List<BackingFieldDefinition>();

		/// <summary>
		/// List of action handlers for registering x:Bind events
		/// </summary>
		public List<EventHandlerBackingFieldDefinition> xBindEventsHandlers { get; } = new();

		/// <summary>
		/// Lists the ElementStub builder holder variables used to pin references for implicit pinning platforms
		/// </summary>
		public List<string> ElementStubHolders { get; } = new List<string>();

		public HashSet<string> ReferencedElementNames { get; } = new HashSet<string>();

		public Dictionary<string, Subclass> Subclasses { get; } = new Dictionary<string, Subclass>();

		public List<ComponentDefinition> Components { get; } = new List<ComponentDefinition>();

		public List<XamlObjectDefinition> XBindExpressions { get; } = new List<XamlObjectDefinition>();

		public List<string> XBindTryGetMethodDeclarations { get; } = new List<string>();

		public int ComponentCount => Components.Count;
	}
}
