using System.Text.RegularExpressions;

Directory.SetCurrentDirectory("C:\\Users\\PC\\Desktop\\uno\\doc\\articles");

var includeFiles = new HashSet<string>();
var nonIncludeMarkdownFiles = new Dictionary<string, MarkdownFile>();

int returnCode = 0;

foreach (var file in Directory.GetFiles(".", "*.md", SearchOption.AllDirectories))
{
    var contents = File.ReadAllText(file);
    foreach (Match m in Regex.Matches(contents, @"\[\!include\s*\[.*?\]\((.+?)\)\]", RegexOptions.IgnoreCase))
    {
        var path = Path.GetRelativePath(".", Path.Join(Path.GetDirectoryName(file), m.Groups[1].Value));
        if (!File.Exists(path))
        {
            Console.WriteLine("UnoHealth001: An include path not found:");
            Console.WriteLine(path);
            Console.WriteLine();
            returnCode = 1;
        }

        includeFiles.Add(path.Replace("\\", "/"));
    }
}

foreach (var file in Directory.GetFiles(".", "*.md", SearchOption.AllDirectories))
{
    var fileAdjusted = Path.GetRelativePath(".", file).Replace("\\", "/");
    if (!includeFiles.Contains(fileAdjusted))
    {
        nonIncludeMarkdownFiles[fileAdjusted] = MarkdownFile.Create(fileAdjusted);
    }
}

var tocFile = File.ReadAllText("toc.yml");
var tocLinkMatches = Regex.Matches(tocFile, "(href|topicHref): (.+?)(\r|\r\n|$)", RegexOptions.Multiline);
var tocLinks = new HashSet<string>();

foreach (Match tocLinkMatch in tocLinkMatches)
{
    tocLinks.Add(tocLinkMatch.Groups[2].Value);
}

foreach (var tocLink in tocLinks)
{
    bool isValid;
    if (tocLink.StartsWith("xref:"))
    {
        isValid = nonIncludeMarkdownFiles.Values.Any(md => md.TryGetUid(out var uid) && uid == tocLink.Substring("xref:".Length));
    }
    else
    {
        isValid = nonIncludeMarkdownFiles.Keys.Contains(tocLink);
    }

    if (!isValid)
    {
        Console.WriteLine($"UnoHealth002: Path '{tocLink}' in toc.yml is not correct");
    }

    if (includeFiles.Contains(tocLink))
    {
        Console.WriteLine($"UnoHealth003: Include files should not be in toc.");
    }
}

foreach (var mdfile in nonIncludeMarkdownFiles.Values)
{
    var isInToc = tocLinks.Contains(mdfile.Path) || (mdfile.TryGetUid(out var uid) && tocLinks.Contains($"xref:{uid}"));
    if (!isInToc)
    {
        Console.WriteLine($"UnoHealth004: File {mdfile.Path} not in toc.");
    }
}

return returnCode;
