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
	[SampleControlInfo("Cursor", "SetCursor")]
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
#if NET461 || __WASM__ || __MACOS__
			Box.SelectionChanged -= HandleSelection;
			Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
#endif
		}

		private void InitList()
		{
			#if NET461 || __WASM__ || __MACOS__
			var _enumval = Enum.GetValues(typeof(Windows.UI.Core.CoreCursorType));
			Box.ItemsSource = _enumval;
			Box.SelectedIndex = 0;

			

			Box.SelectionChanged += HandleSelection;
			#endif
		}

		private void HandleSelection(object sender, object args)
		{
			Txt.Text = "Current selection : " + Box.SelectedItem.ToString();

			Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor((Windows.UI.Core.CoreCursorType)Box.SelectedItem, 0);
		}

		private void ResetTapped(object sender, TappedRoutedEventArgs e)
		{
#if NET461 || __WASM__ || __MACOS__
			Txt.Text = "";

			Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
#endif
		}
	}
}
