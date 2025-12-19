using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls.SpellChecking;

/// <summary>
/// Interface for spell checking services across platforms
/// </summary>
internal interface ISpellCheckService
{
	/// <summary>
	/// Gets whether spell checking is supported on this platform
	/// </summary>
	bool IsSupported { get; }

	/// <summary>
	/// Checks if a word is misspelled
	/// </summary>
	/// <param name="word">The word to check</param>
	/// <returns>True if the word is misspelled, false otherwise</returns>
	Task<bool> IsMisspelledAsync(string word);

	/// <summary>
	/// Gets spelling suggestions for a misspelled word
	/// </summary>
	/// <param name="word">The misspelled word</param>
	/// <returns>List of suggested corrections</returns>
	Task<IReadOnlyList<string>> GetSuggestionsAsync(string word);

	/// <summary>
	/// Adds a word to the user's custom dictionary
	/// </summary>
	/// <param name="word">The word to add</param>
	/// <returns>True if successful, false otherwise</returns>
	Task<bool> AddToDictionaryAsync(string word);

	/// <summary>
	/// Ignores a word for the current session
	/// </summary>
	/// <param name="word">The word to ignore</param>
	void IgnoreWord(string word);
}
