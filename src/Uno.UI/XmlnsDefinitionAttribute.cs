using System;
using System.Diagnostics;
using System.Windows.Markup;

[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml", System.Windows.Markup.XamlConstants.BaseXamlNamespace, AssemblyName = "Uno.UI")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", System.Windows.Markup.XamlConstants.BaseXamlNamespace, AssemblyName = "Uno.UI")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", System.Windows.Markup.XamlConstants.Namespaces.Controls, AssemblyName = "Uno.UI")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", System.Windows.Markup.XamlConstants.Namespaces.Primitives, AssemblyName = "Uno.UI")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", System.Windows.Markup.XamlConstants.Namespaces.Text, AssemblyName = "Uno.UI")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", System.Windows.Markup.XamlConstants.Namespaces.Data, AssemblyName = "Uno.UI")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", System.Windows.Markup.XamlConstants.Namespaces.Documents, AssemblyName = "Uno.UI")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", System.Windows.Markup.XamlConstants.Namespaces.Media, AssemblyName = "Uno.UI")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", System.Windows.Markup.XamlConstants.Namespaces.MediaAnimation, AssemblyName = "Uno.UI")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", System.Windows.Markup.XamlConstants.Namespaces.Shapes, AssemblyName = "Uno.UI")]

[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml", "System", AssemblyName = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml", "System", AssemblyName = "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]

namespace System.Windows.Markup
{
	internal static class XamlConstants
	{
#if HAS_UNO_WINUI
		public const string RootUINamespace = "Microsoft.UI";
#else
		public const string RootUINamespace = "Windows.UI";
#endif

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
			public const string Shapes = BaseXamlNamespace + ".Shapes";

		}
	}
}

