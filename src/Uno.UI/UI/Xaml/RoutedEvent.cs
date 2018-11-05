using System.Runtime.CompilerServices;

namespace Windows.UI.Xaml
{
	public partial class RoutedEvent
	{
		public string Name { get; private set; }

		public RoutedEvent([CallerMemberName] string name = null)
		{
			Name = name;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
