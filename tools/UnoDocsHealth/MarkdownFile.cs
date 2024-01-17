using System.Diagnostics.CodeAnalysis;

public class MarkdownFile
{
    private MarkdownFile(string path, string[] lines, Dictionary<string, string> metadata, int markdownFileStart)
    {
        Path = path;
        Lines = lines;
        Metadata = metadata;
        MarkdownFileStart = markdownFileStart;
    }

    public string Path { get; }
    public string[] Lines { get; }
    public Dictionary<string, string> Metadata { get; }
    public int MarkdownFileStart { get; }

    public static MarkdownFile Create(string path)
    {
        var contents = File.ReadAllLines(path);
        if (contents.Length == 0)
        {
            throw new Exception("Empty file.");
        }

        Dictionary<string, string> metadata = new();
        int markdownLineStart = 0;
        if (contents[0] == "---")
        {
            for (int i = 1; i < contents.Length; i++)
            {
                if (contents[i] == "---")
                {
                    markdownLineStart = i + 1;
                    break;
                }
                var split = contents[i].Split(": ");
                metadata[split[0]] = split[1];
            }

            if (markdownLineStart == 0)
            {
                throw new Exception("File starts with '---' but no corresponding ending '---' was found.");
            }
        }

        return new MarkdownFile(path, contents, metadata, markdownLineStart);
    }

    public bool TryGetUid([NotNullWhen(true)] out string? uid)
    {
        return Metadata.TryGetValue("uid", out uid);
    }
}
