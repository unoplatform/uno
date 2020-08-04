using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Uno.UI.Common;

namespace Windows.UI.Xaml.Controls
{
	internal interface IListView : IItemsControl, IFrameworkElement
	{
		bool IsItemClickEnabled { get; set; }
#if XAMARIN && !__MACOS__
		ICommand ItemClickCommand { get; set; }
#endif
		ItemsPanelTemplate ItemsPanel { get; set; }
		object SelectedItem { get; set; }
	}
}
