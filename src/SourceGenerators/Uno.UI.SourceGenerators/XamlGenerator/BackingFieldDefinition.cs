using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class BackingFieldDefinition
	{
		public BackingFieldDefinition(string type, string name, Accessibility accessibility)
		{
			Type = type;
			Name = name;
			Accessibility = accessibility;
		}

		public string Name { get; }
		public object Type { get; }

		public Accessibility Accessibility { get; }
	}
}
