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
		public string Type { get; }

		public Accessibility Accessibility { get; }

		public override int GetHashCode() => (Type ?? "").GetHashCode() ^ (Name ?? "").GetHashCode() ^ Accessibility.GetHashCode();

		public override bool Equals(object obj)
		{
			return obj is BackingFieldDefinition other
				&& Type == other.Type
				&& Name == other.Name
				&& Accessibility == other.Accessibility;
		}
	}
}
