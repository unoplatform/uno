#if IS_UNIT_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemsControl
	{
		public ItemsPresenter ItemsPresenter => _itemsPresenter;
	}
}
#endif
