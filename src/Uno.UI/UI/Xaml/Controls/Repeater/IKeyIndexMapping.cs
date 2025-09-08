using System;
using System.Linq;

namespace Microsoft.UI.Xaml.Controls
{
	public partial interface IKeyIndexMapping
	{
		string KeyFromIndex(int index);
		int IndexFromKey(string key);
	}
}
