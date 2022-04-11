using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Uno.UI.SourceGenerators.IntegrationTests;

public sealed class FileAdditionalText : AdditionalText
{
    public string Text { get; }

    public override string Path { get; }

	public FileAdditionalText(string path)
	{
		Path = path;
		Text = File.ReadAllText(Path);
	}

    public override SourceText GetText(CancellationToken cancellationToken = default)
    {
        return SourceText.From(Text);
    }
}
