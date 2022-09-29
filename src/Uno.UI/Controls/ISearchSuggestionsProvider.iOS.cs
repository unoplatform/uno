#nullable disable

using Uno;
using System.Threading.Tasks;
using System;
using CT = System.Threading.CancellationToken;

namespace Uno.UI.Controls
{
	public interface ISearchSuggestionsProvider
	{
		Task<SearchSuggestion[]> GetSuggestions(CT ct, string queryText);
	}
}
