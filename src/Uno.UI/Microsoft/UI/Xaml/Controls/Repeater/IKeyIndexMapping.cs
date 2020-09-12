using System;

namespace Microsoft.UI.Xaml.Controls
{
	public interface IKeyIndexMapping
	{
		String KeyFromIndex(Int32 index);

		Int32 IndexFromKey(String key);
	}
}
