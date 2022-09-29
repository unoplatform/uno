#nullable disable

namespace Windows.Foundation.Collections;

	public partial interface IMapChangedEventArgs<K>
	{
		CollectionChange CollectionChange { get; }
		K Key { get; }
	}

