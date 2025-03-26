using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Uno.UI")]
[assembly: InternalsVisibleTo("Uno.UI.Wasm")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.WebAssembly")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("Uno.UI.Tests")]
[assembly: InternalsVisibleTo("Uno.UI.Unit.Tests")]
[assembly: InternalsVisibleTo("Uno.UI.Toolkit")]
[assembly: InternalsVisibleTo("Uno.UI.Composition")]
[assembly: InternalsVisibleTo("Uno.UI.Lottie")]
[assembly: InternalsVisibleTo("Uno.UI.GooglePlay")]
[assembly: InternalsVisibleTo("Uno.UI.Svg")]
[assembly: InternalsVisibleTo("Uno.UI.MediaPlayer.Skia.Gtk")]
[assembly: InternalsVisibleTo("Uno.UI.MediaPlayer.WebAssembly")]
[assembly: InternalsVisibleTo("Uno.UI.XamlHost")]

[assembly: InternalsVisibleTo("Uno.WinUI.Graphics3DGL")]

[assembly: InternalsVisibleTo("SamplesApp")]
[assembly: InternalsVisibleTo("SamplesApp.Droid")]
[assembly: InternalsVisibleTo("SamplesApp.macOS")]
[assembly: InternalsVisibleTo("SamplesApp.Wasm")]
[assembly: InternalsVisibleTo("SamplesApp.Skia")]
[assembly: InternalsVisibleTo("UnoIslandsSamplesApp.Skia")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]

[assembly: Microsoft/* UWP don't rename */.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Windows" /* Keep to avoid renaming */ + ".UI")]

namespace Microsoft/* UWP don't rename */.UI.Xaml;

// This attribute is aligned with https://github.com/dotnet/maui/blob/312948086267cf6c529dfeb2ec0eeae7e7aa57ae/src/Graphics/src/Graphics/XmlnsDefinitionAttribute.cs#L8
// Visual studio now expects this attribute to be present in order to provide intellisense for the types
// in the namespace, and must not have the `Assembly` property.
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[DebuggerDisplay("{XmlNamespace}, {ClrNamespace}")]
internal sealed class XmlnsDefinitionAttribute(string xmlNamespace, string clrNamespace) : Attribute
{
	public string XmlNamespace { get; } = xmlNamespace ?? throw new ArgumentNullException(nameof(clrNamespace));
	public string ClrNamespace { get; } = clrNamespace ?? throw new ArgumentNullException(nameof(xmlNamespace));
}
