using System;
using System.Collections.Generic;
using System.Linq;

namespace Windows.Services.Store
{
	public partial class StoreProductQueryResult
	{
		internal StoreProductQueryResult(Exception exception) =>
			ExtendedError = exception;

		internal StoreProductQueryResult(StoreProduct[] products) =>
			Products = products.ToDictionary(p => p.StoreId, p => p);

		public IReadOnlyDictionary<string, StoreProduct> Products { get; }

		public Exception ExtendedError { get; }
	}
}
