#nullable enable

using System;
using Uno.Collections;

namespace Microsoft.UI.Xaml;

public partial class DependencyObjectStore : IDisposable
{
	private readonly struct AncestorsDictionary
	{
		private readonly HashtableEx _entries = new HashtableEx();

		internal bool TryGetValue(object key, out bool isAncestor)
		{
			if (_entries.TryGetValue(key, out var value))
			{
				isAncestor = (bool)value!;
				return true;
			}

			isAncestor = false;
			return false;
		}

		internal void Set(object key, bool isAncestor)
			=> _entries[key] = isAncestor;

		internal void Clear()
			=> _entries.Clear();

		internal void Dispose()
			=> _entries.Dispose();
	}
}
