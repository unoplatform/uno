#nullable enable

using System.Collections.Generic;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class XLoadScope
	{
		public List<ComponentEntry> Components { get; } = new List<ComponentEntry>();
		public int ComponentCount => Components.Count;

		/// <summary>
		/// List of action handlers for registering x:Bind events
		/// </summary>
		public List<BackingFieldDefinition> xBindEventsHandlers { get; } = new List<BackingFieldDefinition>();

	}
}
