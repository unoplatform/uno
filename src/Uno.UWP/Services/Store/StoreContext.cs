using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Services.Store
{
	public sealed partial class StoreContext
    {
		private StoreContext() { }

		public static StoreContext GetDefault() => new StoreContext();
	}
}
