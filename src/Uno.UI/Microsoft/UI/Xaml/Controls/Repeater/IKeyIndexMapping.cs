using System;
using System.Linq;

namespace Microsoft.UI.Xaml.Controls
{
	public interface IKeyIndexMapping
	{
		string KeyFromIndex(int index);
		int IndexFromKey(string key);
	}
}
