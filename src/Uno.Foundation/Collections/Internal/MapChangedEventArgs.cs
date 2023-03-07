namespace Windows.Foundation.Collections;

internal record class MapChangedEventArgs(CollectionChange CollectionChange, string Key) :
	IMapChangedEventArgs<string>;

