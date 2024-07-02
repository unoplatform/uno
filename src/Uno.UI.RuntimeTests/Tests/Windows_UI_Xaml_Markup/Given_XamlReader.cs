#if HAS_UNO
using System.Linq;
using Uno.UI.Extensions;
using Uno.UI.Helpers;
using Uno.Xaml;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_XamlReader
	{
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
			Assert.ThrowsException<XamlParseException>(() => XamlHelper.LoadXaml<Border>("""
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
			Assert.ThrowsException<XamlParseException>(() => XamlHelper.LoadXaml<Border>("""
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

			Assert.AreEqual(expr.ParentBinding.Path.Path, "(Windows.UI.Xaml.Controls:ScrollViewer.HorizontalScrollMode)");
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
			var xaml = Windows.UI.Xaml.Markup.XamlReader.Load(xamlString);
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
}
#endif
