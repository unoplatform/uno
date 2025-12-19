using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls.SpellChecking;

/// <summary>
/// Default implementation of spell checking service that provides no spell checking
/// Platform-specific implementations should override this to provide actual spell checking
/// </summary>
internal class DefaultSpellCheckService : ISpellCheckService
{
	public static readonly ISpellCheckService Instance = new DefaultSpellCheckService();

	private DefaultSpellCheckService()
	{
	}

	public bool IsSupported => false;

	public Task<bool> IsMisspelledAsync(string word) => Task.FromResult(false);

	public Task<IReadOnlyList<string>> GetSuggestionsAsync(string word) => 
		Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

	public Task<bool> AddToDictionaryAsync(string word) => Task.FromResult(false);

	public void IgnoreWord(string word)
	{
		// No-op in default implementation
	}
}
