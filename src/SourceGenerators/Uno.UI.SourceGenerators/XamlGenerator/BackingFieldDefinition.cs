namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class BackingFieldDefinition
	{
		public BackingFieldDefinition(string type, string name)
		{
			Type = type;
			Name = name;
		}

		public string Name { get; }
		public object Type { get; }
	}
}