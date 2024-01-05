using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Services.Store
{
	public sealed partial class StoreProductResult
	{
		internal StoreProductResult() { }

		public StoreProduct Product { get; internal set; } = null!;

		public Exception ExtendedError { get; internal set; } = null!;

	}
}
