using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace Windows.Storage;

public partial class ApplicationDataContainerSettings : IPropertySet, IObservableMap<string, object>, IDictionary<string, object>, IEnumerable<KeyValuePair<string, object>>
{
	internal ApplicationDataContainerSettings(ApplicationDataContainer container, ApplicationDataLocality locality)
	{
	}

	public uint Size
	{
		get
		{
			throw new global::System.NotImplementedException("The member uint ApplicationDataContainerSettings.Size is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=uint%20ApplicationDataContainerSettings.Size");
		}
	}

	public event global::Windows.Foundation.Collections.MapChangedEventHandler<string, object> MapChanged;

	public void Add(string key, object value)
	{
		throw new global::System.NotSupportedException();
	}

	public bool ContainsKey(string key)
	{
		throw new global::System.NotSupportedException();
	}

	public bool Remove(string key)
	{
		throw new global::System.NotSupportedException();
	}

	public bool TryGetValue(string key, out object value)
	{
		throw new global::System.NotSupportedException();
	}

	public object this[string key]
	{
		get
		{
			throw new global::System.NotSupportedException();
		}
		set
		{
			throw new global::System.NotSupportedException();
		}
	}

	public global::System.Collections.Generic.ICollection<string> Keys
	{
		get
		{
			throw new global::System.NotSupportedException();
		}
	}

	public global::System.Collections.Generic.ICollection<object> Values
	{
		get
		{
			throw new global::System.NotSupportedException();
		}
	}

	public void Add(global::System.Collections.Generic.KeyValuePair<string, object> item)
	{
		throw new global::System.NotSupportedException();
	}

	public void Clear()
	{
		throw new global::System.NotSupportedException();
	}

	public bool Contains(global::System.Collections.Generic.KeyValuePair<string, object> item)
	{
		throw new global::System.NotSupportedException();
	}

	public void CopyTo(global::System.Collections.Generic.KeyValuePair<string, object>[] array, int arrayIndex)
	{
		throw new global::System.NotSupportedException();
	}

	public bool Remove(global::System.Collections.Generic.KeyValuePair<string, object> item)
	{
		throw new global::System.NotSupportedException();
	}

	public int Count
	{
		get
		{
			throw new global::System.NotSupportedException();
		}
	}

	public bool IsReadOnly
	{
		get
		{
			throw new global::System.NotSupportedException();
		}
	}

	public IEnumerator<global::System.Collections.Generic.KeyValuePair<string, object>> GetEnumerator()
	{
		throw new global::System.NotSupportedException();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		throw new global::System.NotSupportedException();
	}
}
