#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Foundation.Collections
{
	public partial interface IPropertySet : IObservableMap<string, object>, IDictionary<string, object>, IEnumerable<KeyValuePair<string, object>>
	{
	}
}
