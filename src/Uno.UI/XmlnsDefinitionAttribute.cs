using System;
using System.Diagnostics;
using System.Windows.Markup;

[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml", Microsoft.UI.Xaml.XamlConstants.BaseXamlNamespace)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.BaseXamlNamespace)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.RootUINamespace)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.Namespaces.Controls)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.Namespaces.Primitives)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.Namespaces.Text)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.Namespaces.Data)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.Namespaces.Documents)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.Namespaces.Media)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.Namespaces.MediaAnimation)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.Namespaces.MediaImaging)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.Namespaces.Shapes)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.Namespaces.Automation)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.Namespaces.AutomationPeers)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.Namespaces.Input)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.Namespaces.Markup)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Microsoft.UI.Xaml.XamlConstants.WindowsUINamespace)]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Windows" /* Keep to avoid renaming */ + ".UI")]

[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml", "System")]
[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml", "System")]

namespace Microsoft.UI.Xaml;

// This attribute is aligned with https://github.com/dotnet/maui/blob/312948086267cf6c529dfeb2ec0eeae7e7aa57ae/src/Graphics/src/Graphics/XmlnsDefinitionAttribute.cs#L8
// Visual studio now expects this attribute to be present in order to provide intellisense for the types
// in the namespace, and must not have the `Assembly` property.
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[DebuggerDisplay("{XmlNamespace}, {ClrNamespace}")]
internal sealed class XmlnsDefinitionAttribute : Attribute
{
	public string XmlNamespace { get; }
	public string ClrNamespace { get; }

	public XmlnsDefinitionAttribute(string xmlNamespace, string clrNamespace)
	{
		ClrNamespace = clrNamespace ?? throw new ArgumentNullException(nameof(xmlNamespace));
		XmlNamespace = xmlNamespace ?? throw new ArgumentNullException(nameof(clrNamespace));
	}
}

internal static class XamlConstants
{
	public const string RootUINamespace = "Microsoft.UI";

	public const string WindowsUINamespace = "Windows.UI";

	public const string BaseXamlNamespace = RootUINamespace + ".Xaml";

	internal static class Namespaces
	{
		public const string Controls = BaseXamlNamespace + ".Controls";
		public const string Primitives = Controls + ".Primitives";
		public const string Text = WindowsUINamespace + ".Text";
		public const string Data = BaseXamlNamespace + ".Data";
		public const string Documents = BaseXamlNamespace + ".Documents";
		public const string Media = BaseXamlNamespace + ".Media";
		public const string MediaAnimation = BaseXamlNamespace + ".Media.Animation";
		public const string MediaImaging = BaseXamlNamespace + ".Media.Imaging";
		public const string Shapes = BaseXamlNamespace + ".Shapes";
		public const string Automation = BaseXamlNamespace + ".Automation";
		public const string AutomationPeers = BaseXamlNamespace + ".Automation.Peers";
		public const string Input = BaseXamlNamespace + ".Input";
		public const string Markup = BaseXamlNamespace + ".Markup";
	}
}
