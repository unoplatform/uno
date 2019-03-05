using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using System;
using Windows.Foundation;

// Pour en savoir plus sur le modèle d'élément Contrôle utilisateur, consultez la page https://go.microsoft.com/fwlink/?LinkId=234236

namespace SamplesApp.Wasm.Windows_UI_Core
{
	[SampleControlInfo("Cursor", "SetCursor")]
	public sealed partial class SetCursor : UserControl
	{
		public SetCursor()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
			this.Unloaded += OnUnLoaded;

		}
		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			this.Loaded -= OnLoaded;

			InitList();
		}


		private void InitList()
		{
			//var _enumval = Enum.GetValues(typeof(Windows.UI.Core.CoreCursorType));
			//Box.ItemsSource = _enumval.;



			//void handleSelection(object sender, object args)
			//{

			//	//var item = (string)Box.SelectedItem;
			//	//Windows.UI.Core.CoreCursorType choice;
			//	//if (Enum.TryParse(item, out choice))
			//	//{
			//	//	Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(choice, 0);
			//	//}

			//	//Box.Loaded += (s, e) => Box.SelectionChanged += handleSelection;
			//	//Box.Unloaded += (s, e) => Box.SelectionChanged -= handleSelection;
			//}
		}

		private void OnUnLoaded(object sender, RoutedEventArgs e)
		{
			this.Unloaded -= OnUnLoaded;

			Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
		}
		private void ResetTapped(object sender, TappedRoutedEventArgs e)
		{
			Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);

		}
	}
}
