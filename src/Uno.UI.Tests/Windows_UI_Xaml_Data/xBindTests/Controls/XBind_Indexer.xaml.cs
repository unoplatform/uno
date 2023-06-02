using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls;

public sealed partial class XBind_Indexer : Page
{
	public ViewModel VM { get; set; } = new();

	public XBind_Indexer()
	{
		this.InitializeComponent();
	}
}

public class MyCustomMap : IObservableMap<string, PersonViewModel>
{
	private Dictionary<string, PersonViewModel> _inner = new();

	public PersonViewModel this[string key]
	{
		get
		{
			return _inner[key];
		}
		set
		{
			_inner[key] = value;
			MapChanged?.Invoke(this, new MapChangedEventArgs(CollectionChange.ItemChanged, key));
		}
	}

	public ICollection<string> Keys => _inner.Keys;

	public ICollection<PersonViewModel> Values => _inner.Values;

	public int Count => _inner.Count;

	public bool IsReadOnly => false;

	public event MapChangedEventHandler<string, PersonViewModel> MapChanged;

	public void Add(string key, PersonViewModel value)
	{
		_inner.Add(key, value);
		MapChanged?.Invoke(this, new MapChangedEventArgs(CollectionChange.ItemInserted, key));
	}

	public void Add(KeyValuePair<string, PersonViewModel> item) => Add(item.Key, item.Value);

	public void Clear()
	{
		_inner.Clear();
		MapChanged?.Invoke(this, new MapChangedEventArgs(CollectionChange.Reset, null));
	}

	public bool Contains(KeyValuePair<string, PersonViewModel> item) => _inner.TryGetValue(item.Key, out var value) && value == item.Value;
	public bool ContainsKey(string key) => _inner.ContainsKey(key);
	public void CopyTo(KeyValuePair<string, PersonViewModel>[] array, int arrayIndex) => throw new System.NotImplementedException();
	public IEnumerator<KeyValuePair<string, PersonViewModel>> GetEnumerator() => _inner.GetEnumerator();
	public bool Remove(string key)
	{
		if (_inner.Remove(key))
		{
			MapChanged?.Invoke(this, new MapChangedEventArgs(CollectionChange.ItemRemoved, key));
			return true;
		}

		return false;
	}

	public bool Remove(KeyValuePair<string, PersonViewModel> item)
	{
		if (Contains(item))
		{
			return Remove(item.Key);
		}

		return false;
	}

	public bool TryGetValue(string key, out PersonViewModel value) => _inner.TryGetValue(key, out value);
	IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
}

public class ViewModel : INotifyPropertyChanged
{
	private ObservableCollection<string> _list = new ObservableCollection<string>() { "ListFirstItem" };
	private PropertySet _dict = new PropertySet() { ["Key"] = "DictionaryValue" };

	private ObservableCollection<PersonViewModel> _list2 = new ObservableCollection<PersonViewModel>() { new PersonViewModel() { Name = "ListFirstItem" } };
	private MyCustomMap _dict2 = new MyCustomMap() { ["Key"] = new PersonViewModel() { Name = "DictionaryValue" } };

	public ObservableCollection<string> List
	{
		get
		{
			return _list;
		}
		set
		{
			_list = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(List)));
		}
	}

	public PropertySet Dict
	{
		get
		{
			return _dict;
		}
		set
		{
			_dict = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Dict)));
		}
	}

	public ObservableCollection<PersonViewModel> List2
	{
		get
		{
			return _list2;
		}
		set
		{
			_list2 = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(List2)));
		}
	}

	public MyCustomMap Dict2
	{
		get
		{
			return _dict2;
		}
		set
		{
			_dict2 = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Dict2)));
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;
}

public class PersonViewModel : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler PropertyChanged;

	private string _name;

	public string Name
	{
		get
		{
			return _name;
		}

		set
		{
			_name = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
		}
	}

}
