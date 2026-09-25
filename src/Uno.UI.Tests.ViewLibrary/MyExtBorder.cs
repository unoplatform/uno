using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.ViewLibrary;

/// <summary>
/// A guest-typed container: tests load a copy of this library into a collectible
/// AssemblyLoadContext and parent shared-framework subtrees under this element.
/// </summary>
public partial class MyExtBorder : Border
{
}
