using Windows.UI.Xaml;

namespace Uno.UI.DataBinding
{
	public class BinderDetails : Java.Lang.Object
	{
		public static bool IsBinderDetailsEnabled { get; set; }

		public BinderDetails(DependencyObject owner)
		{
		}

		public string GetDataContext()
		{
			return null;
		}

		public string GetTemplatedParent()
		{
			return null;
		}

		public Java.Lang.Object[] GetDependencyPropertiesNativeField()
		{
			return null;
		}

		public override string ToString()
		{
			return null;
		}
	}
}
