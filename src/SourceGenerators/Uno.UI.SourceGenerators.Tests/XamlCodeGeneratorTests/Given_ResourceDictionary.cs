using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_ResourceDictionary
{
	[TestMethod]
	public async Task TestTwoLevelNestedMergedDictionariesWithSingleResourceDictionary()
	{
		var xamlFile = new XamlFile("MyResourceDictionary.xaml", """
			<ResourceDictionary
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">
				<ResourceDictionary.MergedDictionaries>
						<ResourceDictionary Source="ms-appx:///Path/To/File1.xaml">
							<ResourceDictionary.MergedDictionaries>
								<ResourceDictionary Source="ms-appx:///Path/To/File2.xaml" />
							</ResourceDictionary.MergedDictionaries>
						</ResourceDictionary>
				</ResourceDictionary.MergedDictionaries>
			</ResourceDictionary>
			""");

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					string.Empty, // https://github.com/dotnet/roslyn-sdk/issues/1121
				}
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task TestTwoLevelNestedMergedDictionariesWithTwoResourceDictionaries()
	{
		var xamlFile = new XamlFile("MyResourceDictionary.xaml", """
			<ResourceDictionary
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">
				<ResourceDictionary.MergedDictionaries>
						<ResourceDictionary Source="ms-appx:///Path/To/File1.xaml">
							<ResourceDictionary.MergedDictionaries>
								<ResourceDictionary Source="ms-appx:///Path/To/File2.xaml" />
								<ResourceDictionary Source="ms-appx:///Path/To/File3.xaml" />
							</ResourceDictionary.MergedDictionaries>
						</ResourceDictionary>
				</ResourceDictionary.MergedDictionaries>
			</ResourceDictionary>
			""");

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					string.Empty, // https://github.com/dotnet/roslyn-sdk/issues/1121
				}
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task TestThreeLevelNestedMergedDictionaries()
	{
		var xamlFile = new XamlFile("MyResourceDictionary.xaml", """
			<ResourceDictionary
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">
				<ResourceDictionary.MergedDictionaries>
						<ResourceDictionary Source="ms-appx:///Path/To/File1.xaml">
							<ResourceDictionary.MergedDictionaries>
								<ResourceDictionary Source="ms-appx:///Path/To/File2.xaml">
									<ResourceDictionary.MergedDictionaries>
										<ResourceDictionary Source="ms-appx:///Path/To/File3.xaml" />
									</ResourceDictionary.MergedDictionaries>
								</ResourceDictionary>
							</ResourceDictionary.MergedDictionaries>
						</ResourceDictionary>
				</ResourceDictionary.MergedDictionaries>
			</ResourceDictionary>
			""");

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					string.Empty, // https://github.com/dotnet/roslyn-sdk/issues/1121
				}
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
