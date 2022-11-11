using System.Collections.Generic;

namespace Windows.Foundation.Collections;

/// <summary>
/// Represents a collection of key-value pairs, correlating several other collection interfaces.
/// </summary>
public partial interface IPropertySet :
	IDictionary<string, object>,
	IEnumerable<KeyValuePair<string, object>>,
	IObservableMap<string, object>
{
}
