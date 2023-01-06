using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Services.Store
{
	public sealed class StoreProductResult
	{
		internal StoreProductResult() { }

		public StoreProduct Product { get; internal set; }

		public Exception ExtendedError { get; internal set; }

	}
}
