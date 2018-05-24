using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class NameScope
	{
		public NameScope(string name)
		{
			this.Name = name;
		}

		public string Name { get; private set; }

		public List<BackingFieldDefinition> BackingFields { get; } = new List<BackingFieldDefinition>();

		public HashSet<string> ReferencedElementNames { get; } = new HashSet<string>();

		public Dictionary<string, Subclass> Subclasses { get; } = new Dictionary<string, Subclass>();
	}
}
