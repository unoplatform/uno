#nullable enable

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
		public const string RootWUINamespace = "Windows" + ".UI"; // Keep split for the WinUI conversion tool
		public const string RootMUINamespace = "Microsoft.UI";
		public const string BaseXamlNamespace = "Microsoft.UI.Xaml";
		public const string UnoXamlNamespace = "Microsoft.UI.Xaml";

		public const int MaxFluentResourcesVersion = 2;

		public static class Namespaces
		{
			public const string Base = BaseXamlNamespace;
			public const string Controls = BaseXamlNamespace + ".Controls";
			public const string Primitives = Controls + ".Primitives";
			public const string Text = RootWUINamespace + ".Text";
			public const string Data = BaseXamlNamespace + ".Data";
			public const string Documents = BaseXamlNamespace + ".Documents";
			public const string Media = BaseXamlNamespace + ".Media";
			public const string MediaAnimation = BaseXamlNamespace + ".Media.Animation";
			public const string MediaImaging = BaseXamlNamespace + ".Media.Imaging";
			public const string Shapes = BaseXamlNamespace + ".Shapes";
			public const string Input = BaseXamlNamespace + ".Input";
			public const string Automation = BaseXamlNamespace + ".Automation";

			public static readonly string[] PresentationNamespaces =
			{
				Controls,
#if HAS_UNO_WINUI
				RootWUINamespace + ".Xaml.Controls", // NavigationViewList is in Microsoft.UI.Xaml.Controls even in WinUI tree
#endif
				Primitives,
				Shapes,
				Input,
				Media,
				MediaAnimation,
				MediaImaging,
				RootWUINamespace,
				BaseXamlNamespace,
				Data,
				Documents,
				Text,
				Automation,
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
			public const string FrameworkElement = UnoXamlNamespace + ".FrameworkElement";
			public const string UIElement = UnoXamlNamespace + ".UIElement";
			public const string Style = BaseXamlNamespace + ".Style";
			public const string ResourceDictionary = BaseXamlNamespace + ".ResourceDictionary";
			public const string ElementStub = BaseXamlNamespace + ".ElementStub";
			public const string ContentPresenter = Namespaces.Controls + ".ContentPresenter";
			public const string Markup = BaseXamlNamespace + ".Markup";
			public const string Metadata = RootFoundationNamespace + ".Metadata";
			public const string IDependencyObjectParse = UnoXamlNamespace + ".IDependencyObjectParse";

			// Attributes
			public const string ContentPropertyAttribute = Markup + ".ContentPropertyAttribute";
			public const string CreateFromStringAttribute = Metadata + ".CreateFromStringAttribute";
			public const string NotImplementedAttribute = "Uno.NotImplementedAttribute";

			// Text
			public const string FontWeight = Namespaces.Text + ".FontWeight";
			public const string FontWeights = RootMUINamespace + ".Text.FontWeights";

			// Misc
			public const string Setter = BaseXamlNamespace + ".Setter";
			public const string CornerRadius = BaseXamlNamespace + ".CornerRadius";
			public const string SolidColorBrushHelper = BaseXamlNamespace + ".SolidColorBrushHelper";
			public const string GridLength = BaseXamlNamespace + ".GridLength";
			public const string GridUnitType = BaseXamlNamespace + ".GridUnitType";
			public const string Color = RootWUINamespace + ".Color";
			public const string Colors = RootMUINamespace + ".Colors";
			public const string ColorHelper = RootMUINamespace + ".ColorHelper";
			public const string Thickness = BaseXamlNamespace + ".Thickness";
			public const string Application = BaseXamlNamespace + ".Application";
			public const string Window = BaseXamlNamespace + ".Window";

			// Media
			public const string LinearGradientBrush = Namespaces.Media + ".LinearGradientBrush";
			public const string Brush = Namespaces.Media + ".Brush";
			public const string SolidColorBrush = Namespaces.Media + ".SolidColorBrush";
			public const string Geometry = Namespaces.Media + ".Geometry";
			public const string Transform = Namespaces.Media + ".Transform";
			public const string KeyTime = Namespaces.MediaAnimation + ".KeyTime";
			public const string Duration = BaseXamlNamespace + ".Duration";
			public const string FontFamily = Namespaces.Media + ".FontFamily";
			public const string ImageSource = Namespaces.Media + ".ImageSource";

			// Controls
			public const string NativePage = Namespaces.Controls + ".NativePage";
			public const string Border = Namespaces.Controls + ".Border";
			public const string TextBlock = Namespaces.Controls + ".TextBlock";
			public const string UserControl = Namespaces.Controls + ".UserControl";
			public const string ContentControl = Namespaces.Controls + ".ContentControl";
			public const string Control = Namespaces.Controls + ".Control";
			public const string Panel = Namespaces.Controls + ".Panel";
			public const string Button = Namespaces.Controls + ".Button";
			public const string Image = Namespaces.Controls + ".Image";
			public const string TextBox = Namespaces.Controls + ".TextBox";
			public const string ColumnDefinition = Namespaces.Controls + ".ColumnDefinition";
			public const string RowDefinition = Namespaces.Controls + ".RowDefinition";

			// EventArgs
			public const string PointerRoutedEventArgs = Namespaces.Input + ".PointerRoutedEventArgs";
			public const string ManipulationStartingRoutedEventArgs = Namespaces.Input + ".ManipulationStartingRoutedEventArgs";
			public const string ManipulationStartedRoutedEventArgs = Namespaces.Input + ".ManipulationStartedRoutedEventArgs";
			public const string ManipulationDeltaRoutedEventArgs = Namespaces.Input + ".ManipulationDeltaRoutedEventArgs";
			public const string ManipulationInertiaStartingRoutedEventArgs = Namespaces.Input + ".ManipulationInertiaStartingRoutedEventArgs";
			public const string ManipulationCompletedRoutedEventArgs = Namespaces.Input + ".ManipulationCompletedRoutedEventArgs";
			public const string TappedRoutedEventArgs = Namespaces.Input + ".TappedRoutedEventArgs";
			public const string DoubleTappedRoutedEventArgs = Namespaces.Input + ".DoubleTappedRoutedEventArgs";
			public const string RightTappedRoutedEventArgs = Namespaces.Input + ".RightTappedRoutedEventArgs";
			public const string HoldingRoutedEventArgs = Namespaces.Input + ".HoldingRoutedEventArgs";
			public const string DragEventArgs = Namespaces.Base + ".DragEventArgs";
			public const string RoutedEventArgs = Namespaces.Base + ".RoutedEventArgs";
			public const string KeyRoutedEventArgs = Namespaces.Input + ".KeyRoutedEventArgs";
			public const string BringIntoViewRequestedEventArgs = Namespaces.Base + ".BringIntoViewRequestedEventArgs";

			// Documents
			public const string Run = Namespaces.Documents + ".Run";
			public const string Span = Namespaces.Documents + ".Span";

			// Markup
			public const string MarkupHelper = "Uno.UI.Helpers.MarkupHelper";
			public const string MarkupXamlBindingHelper = Markup + ".XamlBindingHelper";
			public const string MarkupExtension = Markup + ".MarkupExtension";
			public const string IMarkupExtensionOverrides = Markup + ".IMarkupExtensionOverrides";
		}
	}
}

