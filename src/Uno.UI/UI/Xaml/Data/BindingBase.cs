using System.Collections.Generic;

namespace Windows.UI.Xaml.Data
{
	public partial class BindingBase : DependencyObject
	{
		public BindingBase()
		{
			InitializeBinder();
		}

		public static implicit operator BindingBase(string path)
		{
			return new Binding(path);
		}
	}
}

