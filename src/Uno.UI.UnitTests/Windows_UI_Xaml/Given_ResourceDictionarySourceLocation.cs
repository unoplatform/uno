using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers;
using Uno.UI.Tests.App.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	/// <summary>
	/// Validates the "OriginalSourceLocation" the XAML generator stamps on ResourceDictionary instances,
	/// which is how tooling maps a live dictionary back to the markup that declared it.
	/// </summary>
	/// <remarks>
	/// This project sets UnoForceHotReloadCodeGen, so the locations are always generated here.
	/// </remarks>
	[TestClass]
	public class Given_ResourceDictionarySourceLocation
	{
		private const string DeclaringFile = "ResourceDictionarySourceLocation.xaml";

		private static string GetSourceLocation(ResourceDictionary dictionary)
			=> MarkupHelper.GetElementProperty<string>(dictionary, "OriginalSourceLocation") ?? "";

		[TestMethod]
		public void When_Element_Resources_Then_Location_Is_The_Dictionary_Declaration()
		{
			var sut = new ResourceDictionarySourceLocation();

			StringAssert.EndsWith(GetSourceLocation(sut.Resources), $"{DeclaringFile}#L6:4");
			StringAssert.EndsWith(GetSourceLocation(sut.Element.Resources), $"{DeclaringFile}#L18:5");
		}

		[TestMethod]
		public void When_Merged_Code_Defined_Dictionary_Then_Location_Is_The_Declaring_Markup()
		{
			var sut = new ResourceDictionarySourceLocation();

			// A dictionary defined in code has no location of its own, so the declaration is the only one it gets.
			StringAssert.EndsWith(GetSourceLocation(sut.Resources.MergedDictionaries[0]), $"{DeclaringFile}#L8:6");
		}

		[TestMethod]
		public void When_Merged_Xaml_Dictionary_Then_Location_Is_Its_Own_Declaration()
		{
			var sut = new ResourceDictionarySourceLocation();
			var location = GetSourceLocation(sut.Resources.MergedDictionaries[1]);

			// The dictionary stamps its own declaration site while being constructed, and that one wins
			// over the site referencing it.
			StringAssert.Contains(location, "Subclassed_Dictionary.xaml#L");
			Assert.IsFalse(location.Contains(DeclaringFile), $"Expected the declaration site, got '{location}'");
		}

		[TestMethod]
		public void When_Merged_Dictionary_From_Source_Then_Location_Is_Not_The_Declaring_Markup()
		{
			var sut = new ResourceDictionarySourceLocation();
			var location = GetSourceLocation(sut.Resources.MergedDictionaries[2]);

			// The instance belongs to the referenced file and is shared, so this file must not claim it.
			Assert.IsFalse(location.Contains(DeclaringFile), $"Expected no location of this file, got '{location}'");
		}

		[TestMethod]
		public void When_Application_Resources_Then_Location_Is_The_Dictionary_Declaration()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			StringAssert.Contains(GetSourceLocation(app.Resources), "App.xaml#L");
		}
	}
}
