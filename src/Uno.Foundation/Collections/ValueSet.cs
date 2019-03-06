#if !NET461
#pragma warning disable 67
using System;
using global::System.Collections;
using global::System.Collections.Generic;
using global::System.Runtime.InteropServices;
using global::System.Text;

namespace Windows.Foundation.Collections
{
	public sealed partial class ValueSet : IPropertySet, IObservableMap<string, object>, IDictionary<string, object>, IEnumerable<KeyValuePair<string, object>>
	{
		
	}
}
#endif