using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml
{
	public partial class ResourceDictionary
	{
		/// <summary>
		/// Clears <c>DependencyObject._associatedParent</c> references into collectible
		/// AssemblyLoadContexts on every materialized value of this dictionary (recursing into
		/// nested, merged and theme dictionaries). A shared resource (e.g. a theme brush) first
		/// consumed by a secondary-ALC element records that element as its InheritanceContext
		/// parent; nothing unassociates it when the element's ALC unloads, so the host-lifetime
		/// resource pins the collectible ALC. Called during ALC teardown from
		/// <see cref="Application.CleanupNonDefaultAlcCaches"/>. Lazy (unmaterialized) entries
		/// have no store and are skipped without being materialized.
		/// </summary>
		internal void ClearCollectibleAssociatedParents()
		{
			foreach (var value in _values.Values)
			{
				// A nested ResourceDictionary is itself an DependencyObject, so it must be
				// matched FIRST and recursed into — otherwise it falls into the provider branch below and
				// the associations held by its own values are never swept.
				if (value is ResourceDictionary nested)
				{
					nested.ClearCollectibleAssociatedParents();
				}
				else if (value is DependencyObject store)
				{
					store.ClearCollectibleAssociatedParent();
				}
			}

			var mergedCount = _mergedDictionaries.Count;
			for (var i = 0; i < mergedCount; i++)
			{
				_mergedDictionaries[i].ClearCollectibleAssociatedParents();
			}

			_themeDictionaries?.ClearCollectibleAssociatedParents();

			// The MATERIALIZED active theme dictionary may be reachable only through this cache:
			// when the theme entry in _themeDictionaries is still a lazy stub, the stub is skipped
			// above and the materialized dictionary it produced — holding the live brushes whose
			// stores record cross-ALC associations — would never be visited.
			_activeThemeDictionary?.ClearCollectibleAssociatedParents();
		}
	}
}
