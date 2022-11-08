#nullable enable

using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal record BackingFieldDefinition(string Type, string Name, Accessibility Accessibility);

	internal record EventHandlerBackingFieldDefinition(string Type, string Name, Accessibility Accessibility, string ComponentName)
		: BackingFieldDefinition(Type, Name, Accessibility);
}
