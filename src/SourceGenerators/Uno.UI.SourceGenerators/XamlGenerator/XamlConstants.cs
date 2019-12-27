using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
    internal static class XamlConstants
	{
		public const string XamlXmlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
		public const string PresentationXamlXmlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
		public const string XmlXmlNamespace = "http://www.w3.org/XML/1998/namespace";
		public const string BundleResourcePrefix = "ms-appx:///";

		public const string RootFoundationNamespace = "Windows.Foundation";
		public const string RootUINamespace = "Windows.UI";
		public const string BaseXamlNamespace = RootUINamespace + ".Xaml";
		public const string UnoXamlNamespace = "Windows.UI.Xaml";

		public static class Namespaces
		{
			public const string Controls = BaseXamlNamespace + ".Controls";
			public const string Primitives = Controls + ".Primitives";
			public const string Text = RootUINamespace + ".Text";
			public const string Data = BaseXamlNamespace + ".Data";
			public const string Documents = BaseXamlNamespace + ".Documents";
			public const string Media = BaseXamlNamespace + ".Media";
			public const string MediaAnimation = BaseXamlNamespace + ".Media.Animation";
			public const string Shapes = BaseXamlNamespace + ".Shapes";

			public static readonly string[] PresentationNamespaces =
			{
				Controls,
				Primitives,
				Shapes,
				Media,
				MediaAnimation,
				RootUINamespace,
				BaseXamlNamespace,
				Data,
				Documents,
				"System",
			};

			public static readonly string[] All =
			{
				BaseXamlNamespace,
				Controls,
				Primitives,
				Data,
				Documents,
				Media,
				MediaAnimation,
				Shapes,
				Text,
			};
		}

		public static class Types
		{
			// Basic stuff
			public const string Binding = Namespaces.Data + ".Binding";
			public const string DependencyObject = BaseXamlNamespace + ".DependencyObject";
			public const string DependencyObjectExtensions = BaseXamlNamespace + ".DependencyObjectExtensions";
			public const string DependencyProperty = BaseXamlNamespace + ".DependencyProperty";
			public const string IFrameworkElement = UnoXamlNamespace + ".IFrameworkElement";
			public const string UIElement = UnoXamlNamespace + ".UIElement";
			public const string Style = BaseXamlNamespace + ".Style";
			public const string ElementStub = BaseXamlNamespace + ".ElementStub";
			public const string ContentPresenter = Namespaces.Controls + ".ContentPresenter";
			public const string Markup = BaseXamlNamespace + ".Markup";
			public const string Metadata = RootFoundationNamespace + ".Metadata";

			// Attributes
			public const string ContentPropertyAttribute = Markup + ".ContentPropertyAttribute";
			public const string CreateFromStringAttribute = Metadata + ".CreateFromStringAttribute";

			// Text
			public const string FontWeight = Namespaces.Text + ".FontWeight";
			public const string FontWeights = Namespaces.Text + ".FontWeights";

			// Misc
			public const string Setter = BaseXamlNamespace + ".Setter";
			public const string CornerRadius = BaseXamlNamespace + ".CornerRadius";
			public const string SolidColorBrushHelper = BaseXamlNamespace + ".SolidColorBrushHelper";
			public const string GridLength = BaseXamlNamespace + ".GridLength";
			public const string GridUnitType = BaseXamlNamespace + ".GridUnitType";
			public const string Color = RootUINamespace + ".Color";
			public const string Colors = RootUINamespace + ".Colors";
			public const string Thickness = BaseXamlNamespace + ".Thickness";
			public const string Application = BaseXamlNamespace + ".Application";

			// Media
			public const string LinearGradientBrush = Namespaces.Media + ".LinearGradientBrush";
			public const string Brush = Namespaces.Media + ".Brush";
			public const string SolidColorBrush = Namespaces.Media + ".SolidColorBrush";
			public const string Geometry = Namespaces.Media + ".Geometry";
			public const string Transform = Namespaces.Media + ".Transform";
			public const string KeyTime = Namespaces.MediaAnimation + ".KeyTime";
			public const string Duration = BaseXamlNamespace + ".Duration";
			public const string FontFamily = Namespaces.Media + ".FontFamily";

			// Controls
			public const string NativePage = Namespaces.Controls + ".NativePage";
			public const string Border = Namespaces.Controls + ".Border";
			public const string TextBlock = Namespaces.Controls + ".TextBlock";
			public const string UserControl = Namespaces.Controls + ".UserControl";
			public const string ContentControl = Namespaces.Controls + ".ContentControl";
			public const string Control = Namespaces.Controls + ".Control";
			public const string Panel = Namespaces.Controls + ".Panel";
			public const string Button = Namespaces.Controls + ".Button";
			public const string TextBox = Namespaces.Controls + ".TextBox";

			// Documents
			public const string Run = Namespaces.Documents + ".Run";
			public const string Span = Namespaces.Documents + ".Span";

			// MarkupExtension
			public const string MarkupExtension = Markup + ".MarkupExtension";
			public const string IMarkupExtensionOverrides = Markup + ".IMarkupExtensionOverrides";
			public const string MarkupExtensionReturnTypeAttribute = Markup + ".MarkupExtensionReturnTypeAttribute";
		}
	}
}

