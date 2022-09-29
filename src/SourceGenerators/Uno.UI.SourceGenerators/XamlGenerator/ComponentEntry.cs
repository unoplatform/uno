namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class ComponentEntry
	{
		public ComponentEntry(string variableName, XamlObjectDefinition objectDefinition)
		{
			VariableName = variableName;
			ObjectDefinition = objectDefinition;
		}

		public string VariableName { get; }
		public XamlObjectDefinition ObjectDefinition { get; }
	}
}
