#nullable enable

namespace Uno.UI.SourceGenerators.XamlGenerator;

/// <summary>
/// Definition for Components used for lazy static resources and x:Load marked objects
/// </summary>
internal record ComponentDefinition(XamlObjectDefinition XamlObject, bool IsWeakReference, string MemberName)
{
	/// <summary>
	/// Non-FrameworkElement's resource binding requires an FE ancestor to properly resolve locally inherited resources.
	/// </summary>
	public ComponentDefinition? ResourceContext { get; set; }
}
