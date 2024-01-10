#nullable enable

using System;
using Uno.UI.DataBinding;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Diagnostics.Eventing;
using Uno.Disposables;
using System.Linq;
using System.Threading;
using Uno.Collections;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Microsoft.UI.Xaml.Data;
using Uno.UI;
using System.Collections;

#if __ANDROID__
using View = Android.Views.View;
#elif __IOS__
using View = UIKit.UIView;
#endif

namespace Microsoft.UI.Xaml
{
	public partial class DependencyObjectStore : IDisposable
	{
		private class AncestorsDictionary
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
}
