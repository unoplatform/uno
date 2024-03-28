#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	public partial class DataContextChangedEventArgs
	{
		public DataContextChangedEventArgs(object newValue)
		{
			NewValue = newValue;
		}

		public bool Handled
		{
			get;
			set;
		}

		public object NewValue
		{
			get;
		}
	}
}
