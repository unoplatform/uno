using System;

namespace Windows.Services.Store;

public sealed partial class StoreProductResult
{
	internal StoreProductResult() { }

	public StoreProduct Product { get; internal set; }

	public Exception ExtendedError { get; internal set; }

}
