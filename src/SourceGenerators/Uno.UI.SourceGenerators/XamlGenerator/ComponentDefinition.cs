#nullable enable


namespace Uno.UI.SourceGenerators.XamlGenerator
{
	/// <summary>
	/// Definition for Components used for lazy static resources and x:Load marked objects
	/// </summary>
	internal record ComponentDefinition(XamlObjectDefinition XamlObject, bool IsWeakReference, string MemberName);
}
