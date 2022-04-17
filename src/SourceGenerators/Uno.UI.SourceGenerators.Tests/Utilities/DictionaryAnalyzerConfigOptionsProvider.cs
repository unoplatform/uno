#if NET6_0_OR_GREATER
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Uno.UI.SourceGenerators.Tests;

internal class DictionaryAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
	public override AnalyzerConfigOptions GlobalOptions { get; }
	public Dictionary<string, Dictionary<string, string>> TreeOptions { get; }
    public Dictionary<string, Dictionary<string, string>> AdditionalTextOptions { get; }

    public DictionaryAnalyzerConfigOptionsProvider(
        Dictionary<string, string>? globalOptions = null,
        Dictionary<string, Dictionary<string, string>>? treeOptions = null,
        Dictionary<string, Dictionary<string, string>>? additionalTextOptions = null)
    {
        GlobalOptions = new DictionaryAnalyzerConfigOptions(globalOptions ?? new());
        TreeOptions = treeOptions ?? new();
        AdditionalTextOptions = additionalTextOptions ?? new();
    }

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
    {
        return new DictionaryAnalyzerConfigOptions(TreeOptions[tree.FilePath]);
    }

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
    {
        return new DictionaryAnalyzerConfigOptions(AdditionalTextOptions[textFile.Path]);
    }
}
#endif
