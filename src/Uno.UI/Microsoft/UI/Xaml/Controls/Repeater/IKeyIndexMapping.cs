using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls
{
	public interface IKeyIndexMapping
	{
		String KeyFromIndex(Int32 index);
		Int32 IndexFromKey(String key);
	}
}
