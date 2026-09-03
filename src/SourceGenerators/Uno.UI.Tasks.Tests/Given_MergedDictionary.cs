using AwesomeAssertions;
using Uno.UI.Tasks.BatchMerge;

namespace Uno.UI.Tasks.Tests;

[TestClass]
public class Given_MergedDictionary
{
	private const string ResourceDictionaryOpen =
		"""
		<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
							xmlns:controls="using:Microsoft.UI.Xaml.Controls"
		""";

	private static string Merge(string namespaceDeclarations, string content)
	{
		var dictionary = MergedDictionary.CreateMergedDicionary();
		dictionary.MergeContent($"{ResourceDictionaryOpen}{namespaceDeclarations}>{content}</ResourceDictionary>");
		dictionary.FinalizeXaml();
		return dictionary.ToString();
	}

	[TestMethod]
	public void When_Conditional_Namespace_Then_It_Is_Ignorable()
	{
		var merged = Merge(
			"""
			 xmlns:not_win="http://uno.ui/not_win"
			 xmlns:wasm="http://uno.ui/wasm"
			""",
			"""<not_win:SolidColorBrush x:Key="Brush" Color="Red" />""");

		merged.Should().Contain("""mc:Ignorable="not_win wasm" """.TrimEnd());
	}

	[TestMethod]
	public void When_No_Conditional_Namespace_Then_There_Is_No_Ignorable()
	{
		var merged = Merge("", """<SolidColorBrush x:Key="Brush" Color="Red" />""");

		merged.Should().NotContain("mc:Ignorable");
	}

	[TestMethod]
	public void When_Non_Conditional_Namespace_Then_It_Is_Not_Ignorable()
	{
		// win: and the API-contract prefixes resolve to the presentation namespace, which the
		// WinAppSDK XAML compiler must keep processing rather than ignore.
		var merged = Merge(
			"""
			 xmlns:win="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			 xmlns:contract7Present="http://schemas.microsoft.com/winfx/2006/xaml/presentation?IsApiContractPresent(Windows.Foundation.UniversalApiContract,7)"
			 xmlns:not_win="http://uno.ui/not_win"
			""",
			"""<win:SolidColorBrush x:Key="Brush" Color="Red" />""");

		merged.Should().Contain("""mc:Ignorable="not_win" """.TrimEnd());
	}

	[TestMethod]
	public void When_Legacy_Conditional_Namespace_Host_Then_It_Is_Ignorable()
	{
		var merged = Merge(
			"""
			 xmlns:android="http://nventive.com/android"
			 xmlns:ios="http://platform.uno/ios"
			""",
			"""<SolidColorBrush x:Key="Brush" Color="Red" />""");

		merged.Should().Contain("""mc:Ignorable="android ios" """.TrimEnd());
	}
}
