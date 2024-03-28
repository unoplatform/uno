using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public partial class ItemsControl_ItemsSource_Leak : UserControl, IExtendedLeakTest
	{
		private static ObservableCollection<string> _staticObservableCollection = new();
		private static CollectionViewSource _staticCollectionViewSource = new();

#if HAS_UNO
		private static ObservableVector<string> _staticObservableVector = new();
#endif

		public ItemsControl_ItemsSource_Leak()
		{
			InitializeComponent();
		}

		public Task WaitForTestToComplete()
		{
			control1.ItemsSource = _staticObservableCollection;
			control3.ItemsSource = _staticCollectionViewSource;

#if HAS_UNO
			control2.ItemsSource = _staticObservableVector;
#endif
			return Task.CompletedTask;
		}
	}
}
