#if HAS_UNO
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.UI.Extensions;
using Uno.UI.Helpers;
using Uno.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using static Private.Infrastructure.TestServices;
using System.Threading.Tasks;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_XamlReader
	{
		[TestMethod]
		public void When_DoubleCollection()
		{
			var rectangle = (Rectangle)Microsoft.UI.Xaml.Markup.XamlReader.Load("""
				<Rectangle xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" StrokeDashArray="1,2" />
				""");
			Assert.AreEqual(2, rectangle.StrokeDashArray.Count);
			Assert.AreEqual(1, rectangle.StrokeDashArray[0]);
			Assert.AreEqual(2, rectangle.StrokeDashArray[1]);

			var value = Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(DoubleCollection), "1,2") as DoubleCollection;
			Assert.IsNotNull(value);
			Assert.AreEqual(2, value.Count);
			Assert.AreEqual(1, value[0]);
			Assert.AreEqual(2, value[1]);
		}

		[TestMethod]
		public void When_PointCollection()
		{
			var polygon = (Polygon)Microsoft.UI.Xaml.Markup.XamlReader.Load("""
				<Polygon xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Points="0,1 2,3 4,5" />
				""");
			Assert.AreEqual(3, polygon.Points.Count);
			Assert.AreEqual(new Point(0, 1), polygon.Points[0]);
			Assert.AreEqual(new Point(2, 3), polygon.Points[1]);
			Assert.AreEqual(new Point(4, 5), polygon.Points[2]);

			var value = Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(PointCollection), "0,1 2,3 4,5") as PointCollection;
			Assert.IsNotNull(value);
			Assert.AreEqual(3, value.Count);
			Assert.AreEqual(new Point(0, 1), value[0]);
			Assert.AreEqual(new Point(2, 3), value[1]);
			Assert.AreEqual(new Point(4, 5), value[2]);
		}

		[TestMethod]
		public void When_Enum_HasNumericalValue()
		{
			XamlHelper.LoadXaml<StackPanel>("""<StackPanel Orientation="0" />""");
		}

		[TestMethod]
		public void When_ResDict_ComprehensiveSetup()
		{
			var sut = XamlHelper.LoadXaml<ResourceDictionary>("""
				<ResourceDictionary>

					<ResourceDictionary.ThemeDictionaries>
						<ResourceDictionary x:Key="Light">
							<Color x:Key="Color1">Red</Color>
						</ResourceDictionary>
						<ResourceDictionary x:Key="Dark">
							<Color x:Key="Color1">Green</Color>
						</ResourceDictionary>
					</ResourceDictionary.ThemeDictionaries>
					<ResourceDictionary.MergedDictionaries>
						<ResourceDictionary>
							<Color x:Key="Color2">Blue</Color>
						</ResourceDictionary>
					</ResourceDictionary.MergedDictionaries>
					<Color x:Key="Color3">White</Color>

				</ResourceDictionary>
			""");

			Assert.AreEqual(2, sut.ThemeDictionaries.Count);
			Assert.AreEqual(1, sut.MergedDictionaries.Count);
			Assert.IsTrue(sut.TryGetValue("Color1", out var _, shouldCheckSystem: false), "Failed to resolve key: Color1");
			Assert.IsTrue(sut.TryGetValue("Color2", out var _, shouldCheckSystem: false), "Failed to resolve key: Color2");
			Assert.IsTrue(sut.TryGetValue("Color3", out var _, shouldCheckSystem: false), "Failed to resolve key: Color3");
		}

		[TestMethod]
		public void When_FrameworkElement_Resources_ComprehensiveSetup()
		{
			var setup = XamlHelper.LoadXaml<Border>("""
				<Border>
					<Border.Resources>
						<ResourceDictionary>
			
							<ResourceDictionary.ThemeDictionaries>
								<ResourceDictionary x:Key="Light">
									<Color x:Key="Color1">Red</Color>
								</ResourceDictionary>
								<ResourceDictionary x:Key="Dark">
									<Color x:Key="Color1">Green</Color>
								</ResourceDictionary>
							</ResourceDictionary.ThemeDictionaries>
							<ResourceDictionary.MergedDictionaries>
								<ResourceDictionary>
									<Color x:Key="Color2">Blue</Color>
								</ResourceDictionary>
							</ResourceDictionary.MergedDictionaries>
							<Color x:Key="Color3">White</Color>

						</ResourceDictionary>
					</Border.Resources>
				</Border>
			""");
			var sut = setup.Resources;

			Assert.AreEqual(2, sut.ThemeDictionaries.Count);
			Assert.AreEqual(1, sut.MergedDictionaries.Count);
			Assert.IsTrue(sut.TryGetValue("Color1", out var _, shouldCheckSystem: false), "Failed to resolve key: Color1");
			Assert.IsTrue(sut.TryGetValue("Color2", out var _, shouldCheckSystem: false), "Failed to resolve key: Color2");
			Assert.IsTrue(sut.TryGetValue("Color3", out var _, shouldCheckSystem: false), "Failed to resolve key: Color3");
		}

		// winui observation: FrameworkElement.Resources
		// - can directly nested res-dict which will replace the .Resources instance
		//		^ x:Key'ing this res-dict or not, has no effect on the outcome (except theme-dict ofc)
		// - can directly nested children resources
		//		^ however each item must be x:Key'd, else: XamlCompiler error WMC0060: Dictionary Item 'Color' must have a Key attribute
		// - but, not both a res-dict AND (any child resource OR another res-dict)
		//		^ doing so resulting in: Xaml Internal Error error WMC9999: This Member 'Resources' has more than one item, use the Items property

		[TestMethod]
		public void When_FrameworkElement_Resources_Nest_ResDict()
		{
			var sut = XamlHelper.LoadXaml<Border>("""
				<Border>
					<Border.Resources>

						<ResourceDictionary x:Key="DontCare_And_ShouldntWork">
							<Color x:Key="Asd">SkyBlue</Color>
						</ResourceDictionary>

					</Border.Resources>
				</Border>
			""");

			Assert.IsFalse(sut.Resources.ContainsKey("DontCare_And_ShouldntWork"), "x:Key on res-dict should not work here.");
			Assert.IsTrue(sut.Resources.ContainsKey("Asd"), "Failed to resolve key: Asd");
		}

		[TestMethod]
		public void When_FrameworkElement_Resources_Nest_Resources()
		{
			var sut = XamlHelper.LoadXaml<Border>("""
				<Border>
					<Border.Resources>

						<Color x:Key="Asd">SkyBlue</Color>
						<Color x:Key="Asd2">SkyBlue</Color>

					</Border.Resources>
				</Border>
			""");

			Assert.IsTrue(sut.Resources.ContainsKey("Asd"), "Failed to resolve key: Asd");
			Assert.IsTrue(sut.Resources.ContainsKey("Asd2"), "Failed to resolve key: Asd2");
		}

		[TestMethod]
		public void When_FrameworkElement_Resources_Nest_ResDictAndRes()
		{
			Assert.ThrowsExactly<XamlParseException>(() => XamlHelper.LoadXaml<Border>("""
				<Border>
					<Border.Resources>
			
						<ResourceDictionary x:Key="DontCare_And_ShouldntWork">
							<Color x:Key="Asd">SkyBlue</Color>
						</ResourceDictionary>
						<Color x:Key="Asd2">SkyBlue</Color>

					</Border.Resources>
				</Border>
			"""), "FE.Resources nested both a res-dict and any resource should throw");
		}

		[TestMethod]
		public void When_FrameworkElement_Resources_Nest_ManyResDicts()
		{
			Assert.ThrowsExactly<XamlParseException>(() => XamlHelper.LoadXaml<Border>("""
				<Border>
					<Border.Resources>
			
						<ResourceDictionary x:Key="DontCare_And_ShouldntWork">
							<Color x:Key="Asd">SkyBlue</Color>
						</ResourceDictionary>
						<ResourceDictionary x:Key="DontCare_And_ShouldntWork2">
							<Color x:Key="Asd2">SkyBlue</Color>
						</ResourceDictionary>

					</Border.Resources>
				</Border>
			"""), "FE.Resources nested multiple res-dicts should throw");
		}

		[TestMethod]
		public void When_ResDict_MergedDict() // uno#13100
		{
			var sut = XamlHelper.LoadXaml<ResourceDictionary>("""
				<ResourceDictionary>
					<ResourceDictionary.MergedDictionaries>
			
						<ResourceDictionary>
							<Color x:Key="Asd">SkyBlue</Color>
						</ResourceDictionary>

					</ResourceDictionary.MergedDictionaries>
				</ResourceDictionary>
			""");

			Assert.IsTrue(sut.MergedDictionaries.FirstOrDefault()?.ContainsKey("Asd"), "Failed to resolve key: Asd");
		}

		[TestMethod]
		public void When_CustomResDict_NormalProperty() // uno#13099
		{
			var sut = XamlHelper.LoadXaml<Given_XamlReader_CustomResDict>("""
				<local:Given_XamlReader_CustomResDict xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup">

					<local:Given_XamlReader_CustomResDict.MemberDict>
						<ResourceDictionary>
							<Color x:Key="Asd">SkyBlue</Color>
						</ResourceDictionary>
					</local:Given_XamlReader_CustomResDict.MemberDict>

				</local:Given_XamlReader_CustomResDict>
			""");

			Assert.IsTrue(sut.MemberDict?.ContainsKey("Asd"), "Failed to resolve key: Asd");
		}

		[TestMethod]
		public void When_CustomResDict_NormalProperty_NestedRD() // uno#13099
		{
			var sut = XamlHelper.LoadXaml<Given_XamlReader_CustomResDict>("""
				<local:Given_XamlReader_CustomResDict xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup">

					<local:Given_XamlReader_CustomResDict.MemberDict2>
						<local:Given_XamlReader_CustomResDict2>
							<Color x:Key="Asd">SkyBlue</Color>
						</local:Given_XamlReader_CustomResDict2>
					</local:Given_XamlReader_CustomResDict.MemberDict2>

				</local:Given_XamlReader_CustomResDict>
			""");

			Assert.IsTrue(sut.MemberDict2?.ContainsKey("Asd"), "Failed to resolve key: Asd");
		}

		[TestMethod]
		public void When_CustomResDict_NormalProperty_DirectRes() // uno#13099
		{
			var sut = XamlHelper.LoadXaml<Given_XamlReader_CustomResDict>("""
				<local:Given_XamlReader_CustomResDict xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup">

					<local:Given_XamlReader_CustomResDict.MemberDict2>
						<Color x:Key="Asd">SkyBlue</Color>
					</local:Given_XamlReader_CustomResDict.MemberDict2>

				</local:Given_XamlReader_CustomResDict>
			""");

			Assert.IsTrue(sut.MemberDict2?.ContainsKey("Asd"), "Failed to resolve key: Asd");
		}

		[TestMethod]
		public void When_TemplateBinding_AttachedProperty()
		{
			var setup = new ContentControl
			{
				Content = "asd",
				Style = XamlHelper.LoadXaml<Style>("""
					<Style TargetType="ContentControl">
						<Setter Property="ScrollViewer.HorizontalScrollMode" Value="Disabled" />
						<Setter Property="Template">
							<Setter.Value>
								<ControlTemplate TargetType="ContentControl">
									<ScrollViewer x:Name="SUT" HorizontalScrollMode="{TemplateBinding ScrollViewer.HorizontalScrollMode}">
										<ContentPresenter />
									</ScrollViewer>
								</ControlTemplate>
							</Setter.Value>
						</Setter>
					</Style>
					"""),
			};
			setup.ApplyTemplate();

			Assert.IsTrue(setup.Style.Setters.OfType<Setter>().Any(x => x.Property == ScrollViewer.HorizontalScrollModeProperty), "Style.Setter[ScrollViewer.HorizontalScrollMode] missing");

			var sut = setup.FindFirstDescendant<ScrollViewer>(x => x.Name == "SUT");
			var expr = sut.GetBindingExpression(ScrollViewer.HorizontalScrollModeProperty);

			Assert.AreEqual("(Microsoft.UI.Xaml.Controls:ScrollViewer.HorizontalScrollMode)", expr.ParentBinding.Path.Path);
			Assert.AreEqual(ScrollMode.Disabled, sut.HorizontalScrollMode);
			ScrollViewer.SetHorizontalScrollMode(setup, ScrollMode.Enabled);
			Assert.AreEqual(ScrollMode.Enabled, sut.HorizontalScrollMode);
		}

		[TestMethod]
		public void When_Input_Namespace()
		{
			var xamlString =
"""
<StandardUICommand 
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	Kind="Copy" />
""";
			var xaml = Microsoft.UI.Xaml.Markup.XamlReader.Load(xamlString);
			Assert.IsInstanceOfType(xaml, typeof(StandardUICommand));
		}

		[TestMethod]
		public void When_XMLNS()
		{
			var xamlString = """
				<Style TargetType="FlyoutPresenter" xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup">
					<Setter Property="local:Given_AttachedDP.Prop" Value="1" />
				</Style>
				""";
			var xaml = XamlHelper.LoadXaml<Style>(xamlString);
			Assert.IsInstanceOfType(xaml, typeof(Style));
		}

		[TestMethod]
		public void When_MarkupExtension_FullName_NodeSyntax()
		{
			var value = XamlHelper.LoadXaml<object>("""
				<local:TestMarkupExtension xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup" />
			""");

			Assert.IsInstanceOfType<bool>(value);
			Assert.IsTrue(value as bool? ?? false);
		}

		[TestMethod]
		public void When_MarkupExtension_ShortName_NodeSyntax()
		{
			var value = XamlHelper.LoadXaml<object>("""
				<local:TestMarkup xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup" />
			""");

			Assert.IsInstanceOfType<bool>(value);
			Assert.IsTrue(value as bool? ?? false);
		}

		[TestMethod]
		public void When_MarkupExtension_FullName_AttributeSyntax_Generic()
		{
			var host = XamlHelper.LoadXaml<Border>("""
				<Border Tag="{local:TestMarkupExtension}"
					xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup"
					 />
			""");

			Assert.IsInstanceOfType<bool>(host.Tag);
			Assert.IsTrue(host.Tag as bool? ?? false);
		}

		[TestMethod]
		public void When_MarkupExtension_ShortName_AttributeSyntax_Generic()
		{
			var host = XamlHelper.LoadXaml<Border>("""
				<Border Tag="{local:TestMarkup}"
					xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup"
					 />
			""");

			Assert.IsInstanceOfType<bool>(host.Tag);
			Assert.IsTrue(host.Tag as bool? ?? false);
		}

		[TestMethod]
		public void When_MarkupExtension_FullName_AttributeSyntax_TextBlock()
		{
			var host = XamlHelper.LoadXaml<TextBlock>("""
				<TextBlock Tag="{local:TestMarkupExtension}"
					xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup"
					 />
			""");

			Assert.IsInstanceOfType<bool>(host.Tag);
			Assert.IsTrue(host.Tag as bool? ?? false);
		}

		[TestMethod]
		public void When_MarkupExtension_ShortName_AttributeSyntax_TextBlock()
		{
			var host = XamlHelper.LoadXaml<TextBlock>("""
				<TextBlock Tag="{local:TestMarkup}"
					xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup"
					 />
			""");

			Assert.IsInstanceOfType<bool>(host.Tag);
			Assert.IsTrue(host.Tag as bool? ?? false);
		}

		[TestMethod]
		public void When_MarkupExtension_TextBlock_Inlines_Explicit()
		{
			var host = XamlHelper.LoadXaml<TextBlock>("""
				<TextBlock xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup">
					<TextBlock.Inlines>
						<Run Text="{local:ReturnStringAsd}" />
						<Run Text="{local:ReturnStringAsd}" />
						<Span>
							<Run Text="{local:ReturnStringAsd}" />
						</Span>
					</TextBlock.Inlines>
				</TextBlock>
			""");
			var expectation = """
					0	Run // Text='Asd'
					1	Run // Text='Asd'
					2	Span
					3		Run // Text='Asd'
			""";

			VerifyInlineTree(expectation, host, DescribeInline);
		}

		[TestMethod]
		public void When_MarkupExtension_TextBlock_Inlines_Implicit()
		{
			var host = XamlHelper.LoadXaml<TextBlock>("""
				<TextBlock xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup">
					<Run Text="{local:ReturnStringAsd}" />
					<Run Text="{local:ReturnStringAsd}" />
					<Span>
						<Run Text="{local:ReturnStringAsd}" />
					</Span>
				</TextBlock>
			""");
			// note: when using this "implicit" syntax, a one-space run is inserted between elements of same level.
			var expectation = """
				0	Run // Text='Asd'
				1	Run // Text=' '
				2	Run // Text='Asd'
				3	Run // Text=' '
				4	Span
				5		Run // Text='Asd'
			""";

			VerifyInlineTree(expectation, host, DescribeInline);
		}

#if false
		[TestMethod]
		public void When_MarkupExtension_ServiceProvider_SelfRoot()
		{
			var provider = XamlHelper.LoadXaml<IXamlServiceProvider>("""
				<local:DebugMarkupExtension xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup"/>
			""");
			//IProvideValueTarget
			//	.TargetObject       // crash: AccessViolationException: Attempted to read or write protected memory. This is often an indication that other memory is corrupt.
			//	.TargetProperty     // throws: XamlParseException: Markup extension could not provide value. 
			//IRootObjectProvider
			//	.RootObject         // {DependecyObject}
			//IUriContext
			//	.BaseUri            // null
		}
#endif

		[TestMethod]
		public void When_MarkupExtension_ServiceProvider_DirectDP()
		{
			var setup = XamlHelper.LoadXaml<TextBlock>("""
				<TextBlock
					xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup"
					Text="{local:DebugMarkupExtension Behavior=AssignToTag}"
					/>
			""");
			Assert.IsInstanceOfType<IXamlServiceProvider>(setup.Tag);
			//IProvideValueTarget
			//	.TargetObject       // {Microsoft.UI.Xaml.Controls.TextBlock}
			//	.TargetProperty     // {Microsoft.UI.Xaml.Markup.ProvideValueTargetProperty}
			//		.DeclaringType  // typeof(TextBlock)
			//		.Type           // typeof(string)
			//		.Name           // "Text"
			//IRootObjectProvider
			//	.RootObject         // {Microsoft.UI.Xaml.Controls.TextBlock}
			//IUriContext
			//	.BaseUri            // null
			var sut = (IXamlServiceProvider)setup.Tag;
			var pvt = sut.GetService<IProvideValueTarget>();
			var pvtp = pvt?.TargetProperty as ProvideValueTargetProperty;
			var rop = sut.GetService<IRootObjectProvider>();

			var expectedDP = TextBlock.TextProperty;

			Assert.AreEqual(setup, pvt?.TargetObject, "IProvideValueTarget.TargetObject");
			Assert.AreEqual(expectedDP.OwnerType, pvtp.DeclaringType, "IProvideValueTarget.TargetProperty.DeclaringType");
			Assert.AreEqual(expectedDP.Name, pvtp.Name, "IProvideValueTarget.TargetProperty.Name");
			Assert.AreEqual(expectedDP.Type, pvtp.Type, "IProvideValueTarget.TargetProperty.Type");
			Assert.AreEqual(setup, rop.RootObject, "IRootObjectProvider.RootObject");
		}

		[TestMethod]
		public void When_MarkupExtension_ServiceProvider_InheritedDP()
		{
			var setup = XamlHelper.LoadXaml<TextBlock>("""
				<TextBlock
					xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup"
					Tag="{local:DebugMarkupExtension Behavior=ReturnProvider}"
					/>
			""");
			Assert.IsInstanceOfType<IXamlServiceProvider>(setup.Tag);
			//IProvideValueTarget
			//    .TargetObject       // {Microsoft.UI.Xaml.Controls.TextBlock}
			//    .TargetProperty     // {Microsoft.UI.Xaml.Markup.ProvideValueTargetProperty} <-- diff
			//        .DeclaringType  // typeof(FrameworkElement)
			//        .Type           // typeof(object)
			//        .Name           // "Tag"
			//IRootObjectProvider
			//    .RootObject         // {Microsoft.UI.Xaml.Controls.TextBlock}
			//IUriContext
			//    .BaseUri            // null
			var sut = (IXamlServiceProvider)setup.Tag;
			var pvt = sut.GetService<IProvideValueTarget>();
			var pvtp = pvt?.TargetProperty as ProvideValueTargetProperty;
			var rop = sut.GetService<IRootObjectProvider>();

			var expectedDP = FrameworkElement.TagProperty;

			Assert.AreEqual(setup, pvt?.TargetObject, "IProvideValueTarget.TargetObject");
			Assert.AreEqual(expectedDP.OwnerType, pvtp.DeclaringType, "IProvideValueTarget.TargetProperty.DeclaringType");
			Assert.AreEqual(expectedDP.Name, pvtp.Name, "IProvideValueTarget.TargetProperty.Name");
			Assert.AreEqual(expectedDP.Type, pvtp.Type, "IProvideValueTarget.TargetProperty.Type");
			Assert.AreEqual(setup, rop.RootObject, "IRootObjectProvider.RootObject");
		}

		[TestMethod]
		public void When_MarkupExtension_ServiceProvider_Nested()
		{
			var setup = XamlHelper.LoadXaml<Grid>("""
				<Grid xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup">
					<TextBlock Text="{local:DebugMarkupExtension Behavior=AssignToTag}" />
				</Grid>
			""");
			var host = (TextBlock)setup.Children[0];
			Assert.IsInstanceOfType<IXamlServiceProvider>(host.Tag);
			//IProvideValueTarget
			//    .TargetObject       // {Microsoft.UI.Xaml.Controls.TextBlock}
			//    .TargetProperty     // {Microsoft.UI.Xaml.Markup.ProvideValueTargetProperty}
			//        .DeclaringType  // typeof(TextBlock)
			//        .Type           // typeof(string)
			//        .Name           // "Text"
			//IRootObjectProvider
			//    .RootObject         // {Microsoft.UI.Xaml.Controls.Grid}  <-- diff
			//IUriContext
			//    .BaseUri            // null

			var sut = (IXamlServiceProvider)host.Tag;
			var pvt = sut.GetService<IProvideValueTarget>();
			var pvtp = pvt?.TargetProperty as ProvideValueTargetProperty;
			var rop = sut.GetService<IRootObjectProvider>();

			var expectedDP = TextBlock.TextProperty;

			Assert.AreEqual(host, pvt?.TargetObject, "IProvideValueTarget.TargetObject");
			Assert.AreEqual(expectedDP.OwnerType, pvtp.DeclaringType, "IProvideValueTarget.TargetProperty.DeclaringType");
			Assert.AreEqual(expectedDP.Name, pvtp.Name, "IProvideValueTarget.TargetProperty.Name");
			Assert.AreEqual(expectedDP.Type, pvtp.Type, "IProvideValueTarget.TargetProperty.Type");
			Assert.AreEqual(setup, rop.RootObject, "IRootObjectProvider.RootObject");
		}

		[TestMethod]
		public void When_MarkupExtension_ServiceProvider_InlineLiteral()
		{
			var rootGrid = XamlHelper.LoadXaml<Grid>("""
				<Grid xmlns:local="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup">
					<StackPanel>
						<local:DebugMarkupExtension Behavior="ReturnTextBlockWithProviderInTag" />
					</StackPanel>
				</Grid>
			""");
			var stackPanel = (StackPanel)rootGrid.Children[0];
			var textBlock = (TextBlock)stackPanel.Children[0];

			Assert.IsInstanceOfType<IXamlServiceProvider>(textBlock.Tag);
			//IProvideValueTarget
			//    .TargetObject       // {Microsoft.UI.Xaml.Controls.StackPanel}
			//    .TargetProperty     // {Microsoft.UI.Xaml.Markup.ProvideValueTargetProperty}
			//        .DeclaringType  // typeof(Panel)
			//        .Type           // typeof(UIElementCollection)
			//        .Name           // "Children"
			//IRootObjectProvider
			//    .RootObject         // {Microsoft.UI.Xaml.Controls.Grid}
			//IUriContext
			//    .BaseUri            // null

			var sut = (IXamlServiceProvider)textBlock.Tag;
			var pvt = sut.GetService<IProvideValueTarget>();
			var pvtp = pvt?.TargetProperty as ProvideValueTargetProperty;
			var rop = sut.GetService<IRootObjectProvider>();

			var expectedDP = new // note: there is no such property in WinUI neither
			{
				OwnerType = typeof(Panel),
				Name = nameof(Panel.Children),
				Type = typeof(UIElementCollection),
			};

			Assert.AreEqual(stackPanel, pvt?.TargetObject, "IProvideValueTarget.TargetObject");
			Assert.AreEqual(expectedDP.OwnerType, pvtp.DeclaringType, "IProvideValueTarget.TargetProperty.DeclaringType");
			Assert.AreEqual(expectedDP.Name, pvtp.Name, "IProvideValueTarget.TargetProperty.Name");
			Assert.AreEqual(expectedDP.Type, pvtp.Type, "IProvideValueTarget.TargetProperty.Type");
			Assert.AreEqual(rootGrid, rop.RootObject, "IRootObjectProvider.RootObject");
		}

		[TestMethod]
		public void When_Invalid_Enum_Value_Template()
		{
			var ex = Assert.ThrowsExactly<XamlParseException>(() =>
				XamlHelper.LoadXaml<StackPanel>(
					"""
					<ContentControl>
						<ContentControl.ContentTemplate>
							<DataTemplate>
								<Border 
									xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup" 
									local:AttachedDPWithEnum.Value="Invalid" />
							</DataTemplate>
						</ContentControl.ContentTemplate>
					</ContentControl>
					""")
			);

			Assert.AreEqual(4, ex.LineNumber);
			Assert.AreEqual("Requested value 'Invalid' was not found.", ex.InnerException.Message);
		}

		[TestMethod]
		public void When_Invalid_Enum_Value_Template_Nested()
		{
			var ex = Assert.ThrowsExactly<XamlParseException>(() =>
				XamlHelper.LoadXaml<StackPanel>(
					"""
					<ContentControl>
						<ContentControl.ContentTemplate>
							<DataTemplate>
								<StackPanel>
									<Border 
										xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup" 
										local:AttachedDPWithEnum.Value="Invalid" />
									<ContentControl>
										<ContentControl.ContentTemplate>
											<DataTemplate>
												<Border 
													xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup" 
													local:AttachedDPWithEnum.Value="Invalid2" />
											</DataTemplate>
										</ContentControl.ContentTemplate>
									</ContentControl>
								</StackPanel>
							</DataTemplate>
						</ContentControl.ContentTemplate>
					</ContentControl>
					""")
			);

			var aggregateException = (AggregateException)ex.InnerException;
			Assert.AreEqual(5, (aggregateException.InnerExceptions[0] as XamlParseException).LineNumber);
			Assert.AreEqual("Requested value 'Invalid' was not found. [Line: 5 Position: 6]", (aggregateException.InnerExceptions[0] as XamlParseException).Message);
			Assert.AreEqual(11, (aggregateException.InnerExceptions[1] as XamlParseException).LineNumber);
			Assert.AreEqual("Requested value 'Invalid2' was not found. [Line: 11 Position: 9]", (aggregateException.InnerExceptions[1] as XamlParseException).Message);
		}

		[TestMethod]
		public void When_Invalid_Ampersand_Escape()
		{
			Assert.ThrowsExactly<System.Xml.XmlException>(() =>
				XamlHelper.LoadXaml<StackPanel>(
					"""
					<ContentControl Content="Hello & Welcome" />
					""")
			);
		}

		[TestMethod]
		public void When_Invalid_Root()
		{
			Assert.ThrowsExactly<XamlParseException>(() =>
				XamlHelper.LoadXaml<StackPanel>(
					"""
					<StackPanel Orientation="invalid" />
					""")
			);
		}

		[TestMethod]
		public void When_Unknown_Type()
		{
			Assert.ThrowsExactly<XamlParseException>(() =>
				XamlHelper.LoadXaml<StackPanel>(
					"""
					<StarPanel Orientation="invalid" />
					""")
			);
		}

		[TestMethod]
		public void When_Unknown_Type_In_Template()
		{
			Assert.ThrowsExactly<XamlParseException>(() =>
				XamlHelper.LoadXaml<StackPanel>(
					"""
					<ContentControl>
						<ContentControl.ContentTemplate>
							<DataTemplate>
								<StarPanel Orientation="invalid" />
							</DataTemplate>
						</ContentControl.ContentTemplate>
					</ContentControl>
					""")
			);
		}

		[TestMethod]
		public void When_Unknown_Property()
		{
			static void Load()
			{
				XamlHelper.LoadXaml<StackPanel>(
					"""
					<StackPanel Oriented="invalid" />
					""");
			}

			if (FeatureConfiguration.XamlReader.FailOnUnknownProperties)
			{
				Assert.ThrowsExactly<XamlParseException>(() => Load());
			}
			else
			{
				Load();
			}
		}

		[TestMethod]
		public void When_Unknown_Property_In_Template()
		{
			static void Load()
			{
				XamlHelper.LoadXaml<StackPanel>(
				"""
				<ContentControl>
					<ContentControl.ContentTemplate>
						<DataTemplate>
							<StackPanel Oriented="invalid" />
						</DataTemplate>
					</ContentControl.ContentTemplate>
				</ContentControl>
				""");
			}

			if (FeatureConfiguration.XamlReader.FailOnUnknownProperties)
			{
				Assert.ThrowsExactly<XamlParseException>(() => Load());
			}
			else
			{
				Load();
			}
		}

		[TestMethod]
		public void When_Multiple_Exceptions_1()
		{
			var ex = Assert.ThrowsExactly<XamlParseException>(() =>
				XamlHelper.LoadXaml<StackPanel>(
					"""
					<StackPanel>
						<StackPanel Orientation="invalid" />
						<StackPanel Orientation="invalid" />
					</StackPanel>
					""")
			);

			Assert.IsInstanceOfType(ex.InnerException, typeof(AggregateException));
			if (ex.InnerException is AggregateException ae)
			{
				Assert.HasCount(2, ae.InnerExceptions);
				Assert.IsInstanceOfType<XamlParseException>(ae.InnerExceptions[0]);
				Assert.IsInstanceOfType<XamlParseException>(ae.InnerExceptions[1]);
				Assert.AreEqual(2, (ae.InnerExceptions[0] as XamlParseException).LineNumber);
				Assert.AreEqual(3, (ae.InnerExceptions[0] as XamlParseException).LinePosition);
				Assert.AreEqual(3, (ae.InnerExceptions[1] as XamlParseException).LineNumber);
				Assert.AreEqual(3, (ae.InnerExceptions[1] as XamlParseException).LinePosition);
			}
		}

		[TestMethod]
		public void When_Multiple_Exceptions_Nested_1()
		{
			var ex = Assert.ThrowsExactly<XamlParseException>(() =>
				XamlHelper.LoadXaml<StackPanel>(
					"""
					<StackPanel>
						<StackPanel Orientation="invalid" />
						<StackPanel>
							<StackPanel Orientation="invalid" />
						</StackPanel>
					</StackPanel>
					""")
			);

			Assert.IsInstanceOfType(ex.InnerException, typeof(AggregateException));
			if (ex.InnerException is AggregateException ae)
			{
				Assert.HasCount(2, ae.InnerExceptions);
				Assert.IsInstanceOfType<XamlParseException>(ae.InnerExceptions[0]);
				Assert.IsInstanceOfType<XamlParseException>(ae.InnerExceptions[1]);
				Assert.AreEqual(2, (ae.InnerExceptions[0] as XamlParseException).LineNumber);
				Assert.AreEqual(3, (ae.InnerExceptions[0] as XamlParseException).LinePosition);
				Assert.AreEqual(4, (ae.InnerExceptions[1] as XamlParseException).LineNumber);
				Assert.AreEqual(4, (ae.InnerExceptions[1] as XamlParseException).LinePosition);
			}
		}

		[TestMethod]
		public void When_Multiple_Exceptions_InvalidFormats()
		{
			var ex = Assert.ThrowsExactly<XamlParseException>(() =>
				XamlHelper.LoadXaml<StackPanel>(
					"""
					<StackPanel>
						<StackPanel Grid.Row="0.5" />
						<StackPanel>
							<StackPanel Grid.Row="invalid" />
						</StackPanel>
					</StackPanel>
					""")
			);

			Assert.IsInstanceOfType(ex.InnerException, typeof(AggregateException));
			if (ex.InnerException is AggregateException ae)
			{
				Assert.HasCount(2, ae.InnerExceptions);
				Assert.IsInstanceOfType<XamlParseException>(ae.InnerExceptions[0]);
				Assert.IsInstanceOfType<XamlParseException>(ae.InnerExceptions[1]);
				Assert.AreEqual(2, (ae.InnerExceptions[0] as XamlParseException).LineNumber);
				Assert.AreEqual(3, (ae.InnerExceptions[0] as XamlParseException).LinePosition);
				Assert.AreEqual(4, (ae.InnerExceptions[1] as XamlParseException).LineNumber);
				Assert.AreEqual(4, (ae.InnerExceptions[1] as XamlParseException).LinePosition);
			}
		}

		[TestMethod]
		public async Task When_ListView_ItemsPanelTemplate()
		{
			var sut = XamlHelper.LoadXaml<ListView>("""
				<ListView>
					<x:String>Hello</x:String>
					<ListView.ItemsPanel>
						<ItemsPanelTemplate>
							<StackPanel Orientation="Horizontal" />
						</ItemsPanelTemplate>
					</ListView.ItemsPanel>
				</ListView>
			""");

			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
		}

		[TestMethod]
		public async Task When_ItemsControl_ItemsPanelTemplate_In_DataTemplate()
		{
			var sut = XamlHelper.LoadXaml<ContentControl>("""
				<ContentControl Content="test" Width="100" Height="100">
					<ContentControl.ContentTemplate>
						<DataTemplate>
							<ItemsControl ItemsSource="{Binding Assets}">
								<ItemsControl.ItemsPanel>
									<ItemsPanelTemplate>
										<StackPanel Orientation="Horizontal"/>
									</ItemsPanelTemplate>
								</ItemsControl.ItemsPanel>
							</ItemsControl>
						</DataTemplate>
					</ContentControl.ContentTemplate>
				</ContentControl>
			""");

			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
		}

		[TestMethod]
		public async Task When_ControlTemplate_In_DataTemplate()
		{
			var sut = XamlHelper.LoadXaml<ContentControl>("""
				<ContentControl Content="test" Width="100" Height="100">
					<ContentControl.ContentTemplate>
						<DataTemplate>
							<ContentControl Content="test2" Width="100" Height="100">
								<ContentControl.Template>
									<ControlTemplate TargetType="ContentControl">
										<Border Background="LightGray">
											<ContentPresenter />
										</Border>
									</ControlTemplate>
								</ContentControl.Template>
							</ContentControl>
						</DataTemplate>
					</ContentControl.ContentTemplate>
				</ContentControl>
			""");

			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
		}
	}

	public partial class Given_XamlReader
	{
		private static void VerifyInlineTree(string expectedTree, TextBlock tb, Func<Inline, IEnumerable<string>> describe)
		{
			var expectations = expectedTree.Split('\n', StringSplitOptions.TrimEntries);
			var descendants = FlattenInlines(tb.Inlines).ToArray();

			Assert.AreEqual(expectations.Length, descendants.Length, "Mismatched descendant size");
			for (int i = 0; i < expectations.Length; i++)
			{
				var line = expectations[i].TrimStart("0123456789. ".ToArray());
				var parts = line.Split(" // ", count: 2);
				var depth = parts[0].TakeWhile(x => x == '\t').Count() - 1; // first tab is used as separator
				parts[0] = parts[0].TrimStart('\t');

				var node = descendants[i];
				var name = node.Inline.GetType().Name;
				var description = string.Join(", ", describe(node.Inline));

				Assert.AreEqual(depth, node.Depth, $"Incorrect depth on line {i}");
				Assert.AreEqual(parts[0], name, $"Incorrect node on line {i}");
				Assert.AreEqual(parts.ElementAtOrDefault(1) ?? "", description, $"Invalid description on line {i}");
			}
		}

		private static IEnumerable<(int Depth, Inline Inline)> FlattenInlines(InlineCollection inlines, int depth = 0)
		{
			// depth first traverse
			foreach (var inline in inlines)
			{
				yield return (depth, inline);

				if (inline is Span span)
				{
					foreach (var nested in FlattenInlines(span.Inlines, depth + 1))
					{
						yield return nested;
					}
				}
			}
		}

		private static IEnumerable<string> DescribeInline(Inline inline)
		{
			if (inline is Run run) yield return $"Text='{run.Text}'";
		}
	}

	public static partial class Given_AttachedDP
	{
		public static void SetProp(this UIElement element, int prop)
		{
			element.SetValue(PropProperty, prop);
		}

		public static double GetProp(this UIElement element)
		{
			return (double)element.GetValue(PropProperty);
		}

		public static DependencyProperty PropProperty { get; } =
			DependencyProperty.RegisterAttached(
				"Prop",
				typeof(int),
				typeof(Given_AttachedDP),
				new PropertyMetadata(0)
			);

	}

	public class Given_XamlReader_CustomResDict : ResourceDictionary
	{
		public ResourceDictionary MemberDict { get; set; } = new();
		public Given_XamlReader_CustomResDict2 MemberDict2 { get; set; } = new();
	}

	public class Given_XamlReader_CustomResDict2 : ResourceDictionary
	{
	}

	public class TestMarkupExtension : MarkupExtension
	{
		public object ServiceProvider { get; private set; }
		public bool ProvideValueCalled { get; private set; }

		protected override object ProvideValue(IXamlServiceProvider serviceProvider)
		{
			ServiceProvider = serviceProvider;

			return ProvideValue();
		}
		protected override object ProvideValue()
		{
			return ProvideValueCalled = true;
		}
	}

	public class ReturnStringAsd : MarkupExtension
	{
		protected override object ProvideValue() => "Asd";
	}

	public static class DebugMarkupAttachable
	{
		#region DependencyProperty: Value

		public static DependencyProperty ValueProperty { get; } = DependencyProperty.RegisterAttached(
			"Value",
			typeof(object),
			typeof(DebugMarkupAttachable),
			new PropertyMetadata(default(object)));

		public static object GetValue(DependencyObject obj) => (object)obj.GetValue(ValueProperty);
		public static void SetValue(DependencyObject obj, object value) => obj.SetValue(ValueProperty, value);

		#endregion
		#region DependencyProperty: Value2

		public static DependencyProperty Value2Property { get; } = DependencyProperty.RegisterAttached(
			"Value2",
			typeof(int),
			typeof(DebugMarkupAttachable),
			new PropertyMetadata(default(int)));

		public static int GetValue2(DependencyObject obj) => (int)obj.GetValue(Value2Property);
		public static void SetValue2(DependencyObject obj, int value) => obj.SetValue(Value2Property, value);

		#endregion
	}

	public static class AttachedDPWithEnum
	{
		public static DependencyProperty ValueProperty { get; } = DependencyProperty.RegisterAttached(
			"Value",
			typeof(ScrollMode),
			typeof(AttachedDPWithEnum),
			new PropertyMetadata(default(ScrollMode)));

		public static ScrollMode GetValue(DependencyObject obj) => (ScrollMode)obj.GetValue(ValueProperty);
		public static void SetValue(DependencyObject obj, ScrollMode value) => obj.SetValue(ValueProperty, value);
	}

	public class DebugMarkupExtension : MarkupExtension
	{
		public enum DebugBehavior { ReturnProvider, ReturnTextBlockWithProviderInTag, AssignToTag, AssignToAttachableValue }

		public DebugBehavior Behavior { get; set; } = DebugBehavior.ReturnProvider;
		protected override object ProvideValue(IXamlServiceProvider serviceProvider)
		{
			if (Behavior == DebugBehavior.ReturnProvider)
			{
				return serviceProvider;
			}
			else if (Behavior == DebugBehavior.ReturnTextBlockWithProviderInTag)
			{
				return new TextBlock { Tag = serviceProvider };
			}
			else
			{
				if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget pvt &&
					pvt.TargetObject is FrameworkElement fe &&
					pvt.TargetProperty is ProvideValueTargetProperty pvtp)
				{
					if (Behavior == DebugBehavior.AssignToTag)
					{
						fe.Tag = serviceProvider;
					}
					else
					{
						DebugMarkupAttachable.SetValue(fe, serviceProvider);
					}

					return pvtp.Type.IsValueType ? Activator.CreateInstance(pvtp.Type) : null;
				}

				return DependencyProperty.UnsetValue;
			}
		}
	}
}
#endif
