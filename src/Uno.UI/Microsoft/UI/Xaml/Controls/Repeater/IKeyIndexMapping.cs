using System;
using System.Linq;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial interface IKeyIndexMapping
	{
		string KeyFromIndex(int index);
		int IndexFromKey(string key);
	}
}
