using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class NameScope
	{
		public NameScope(string @namespace, string className)
		{
			Namespace = @namespace ?? string.Empty;
			ClassName = className;
		}

		public string Name => $"{Namespace.Replace(".", "")}{ClassName}";
		public string Namespace { get; private set; }
		public string ClassName { get; private set; }

		public List<BackingFieldDefinition> BackingFields { get; } = new List<BackingFieldDefinition>();

		public HashSet<string> ReferencedElementNames { get; } = new HashSet<string>();

		public Dictionary<string, Subclass> Subclasses { get; } = new Dictionary<string, Subclass>();

		public List<XamlObjectDefinition> Components { get; } = new List<XamlObjectDefinition>();

		public List<XamlObjectDefinition> XBindExpressions { get; } = new List<XamlObjectDefinition>();

		public int ComponentCount => Components.Count;
	}
}
