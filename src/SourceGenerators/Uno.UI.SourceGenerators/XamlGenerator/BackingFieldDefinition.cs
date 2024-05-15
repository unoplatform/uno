#nullable enable

using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal record BackingFieldDefinition(string GlobalizedTypeName, string Name, Accessibility Accessibility);

	internal record EventHandlerBackingFieldDefinition(string GlobalizedTypeName, string Name, Accessibility Accessibility, string ComponentName)
		: BackingFieldDefinition(GlobalizedTypeName, Name, Accessibility);
}
