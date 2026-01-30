using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Documents;

internal interface ISpellCheckingService
{
	public List<(int correctionStart, int correctionEnd)?> SpellCheck(List<int> wordBoundaries, string text);

	public (int replaceIndexStart, int replaceIndexEnd, List<string> suggestions)? GetSpellCheckSuggestions(string text, List<int> wordBoundaries, int correctionStart, int correctionEnd);
}
