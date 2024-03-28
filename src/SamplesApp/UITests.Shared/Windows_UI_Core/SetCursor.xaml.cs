using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using System.Collections.Generic;
using System;
using Windows.Foundation;

// Pour en savoir plus sur le modèle d'élément Contrôle utilisateur, consultez la page https://go.microsoft.com/fwlink/?LinkId=234236

namespace SamplesApp.Wasm.Windows_UI_Core
{
	[SampleControlInfo("Windows.UI.Core", "SetCursor", isManualTest: true, description: "Demonstrates use of CoreWindow.PointerCursor / CoreCursor / CoreCursorType")]
	public sealed partial class SetCursor : Page
	{
		public SetCursor()
		{
			this.InitializeComponent();
			Box.Loaded += OnLoaded;
			Box.Unloaded += OnUnLoaded;
			this.DataContext = this;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Box.Loaded -= OnLoaded;
			InitList();
		}

		private void OnUnLoaded(object sender, RoutedEventArgs e)
		{
#if !__ANDROID__ && !__IOS__
			Box.SelectionChanged -= HandleSelection;
			Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new global::Windows.UI.Core.CoreCursor(global::Windows.UI.Core.CoreCursorType.Arrow, 0);
#endif
		}

		private void InitList()
		{
#if !__ANDROID__ && !__IOS__
			var _enumval = Enum.GetValues(typeof(global::Windows.UI.Core.CoreCursorType));
			Box.ItemsSource = _enumval;
			Box.SelectedIndex = 0;



			Box.SelectionChanged += HandleSelection;
#endif
		}

		private void HandleSelection(object sender, object args)
		{
			Txt.Text = "Current selection : " + Box.SelectedItem.ToString();

			Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new global::Windows.UI.Core.CoreCursor((global::Windows.UI.Core.CoreCursorType)Box.SelectedItem, 0);
		}

		private void ResetTapped(object sender, TappedRoutedEventArgs e)
		{
#if !__ANDROID__ && !__IOS__
			Txt.Text = "";

			Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new global::Windows.UI.Core.CoreCursor(global::Windows.UI.Core.CoreCursorType.Arrow, 0);
#endif
		}
	}
}
