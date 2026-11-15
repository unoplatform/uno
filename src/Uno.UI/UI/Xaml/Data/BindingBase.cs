using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Data
{
	public partial class BindingBase : DependencyObject
	{
		public BindingBase()
		{
			InitializeBinder();
		}
	}
}

