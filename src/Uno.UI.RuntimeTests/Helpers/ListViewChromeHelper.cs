#if HAS_UNO
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Disposables;

namespace Uno.UI.RuntimeTests.Helpers;

internal static class ListViewChromeHelper
{
	private const string RoundedKey = "ListViewBaseItemRoundedChromeEnabled";

	/// <summary>
	/// Reaches non-rounded chrome the way WinUI TAEF does: resource set to False and the cache cleared.
	/// </summary>
	public static IDisposable UseNonRoundedChrome() => UseRoundedChromeResource(false);

	public static IDisposable UseRoundedChromeResource(bool value)
	{
		var resources = Application.Current.Resources;
		var hadValue = resources.ContainsKey(RoundedKey);
		var previous = hadValue ? resources[RoundedKey] : null;

		resources[RoundedKey] = value;
		ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();

		return Disposable.Create(() =>
		{
			if (hadValue)
			{
				resources[RoundedKey] = previous;
			}
			else
			{
				resources.Remove(RoundedKey);
			}

			ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();
		});
	}
}
#endif
