using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SampleControl.Presentation;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Input;
using System.Threading;
using SampleControl.Entities;
using Windows.System;

#if WINAPPSDK
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
#elif XAMARIN || UNO_REFERENCE_API
using Microsoft.UI.Xaml.Controls;
using System.Globalization;
#endif

namespace Uno.UI.Samples.Controls
{
	public sealed partial class SampleChooserControl : UserControl
	{
		private bool _initialMeasure = true;
		private bool _initialArrange = true;

		public SampleChooserControl()
		{
			this.InitializeComponent();
		}

		private SampleChooserViewModel ViewModel => (SampleChooserViewModel)DataContext;

		private void FocusSearchAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ViewModel is null || !ViewModel.KeyboardShortcutsEnabled)
			{
				return;
			}

			// Ensure the pane is open before focusing the search box
			if (!SplitView.IsPaneOpen)
			{
				SplitView.IsPaneOpen = true;
			}

			SearchBox.Focus(FocusState.Keyboard);
			args.Handled = true;
		}

		private void ReloadSampleAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ViewModel is null || !ViewModel.KeyboardShortcutsEnabled)
			{
				return;
			}

			if (ViewModel.ReloadCurrentTestCommand.CanExecute(null))
			{
				ViewModel.ReloadCurrentTestCommand.Execute(null);
			}
			args.Handled = true;
		}

		private void PreviousSampleAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ViewModel is null || !ViewModel.KeyboardShortcutsEnabled)
			{
				return;
			}

			if (ViewModel.LoadPreviousTestCommand.CanExecute(null))
			{
				ViewModel.LoadPreviousTestCommand.Execute(null);
			}
			args.Handled = true;
		}

		private void NextSampleAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ViewModel is null || !ViewModel.KeyboardShortcutsEnabled)
			{
				return;
			}

			if (ViewModel.LoadNextTestCommand.CanExecute(null))
			{
				ViewModel.LoadNextTestCommand.Execute(null);
			}
			args.Handled = true;
		}

		private void FavoritesViewAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ViewModel is null || !ViewModel.KeyboardShortcutsEnabled)
			{
				return;
			}

			ViewModel.ShowNewSectionCommand.Execute("Favorites");
			args.Handled = true;
		}

		private void HistoryViewAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ViewModel is null || !ViewModel.KeyboardShortcutsEnabled)
			{
				return;
			}

			ViewModel.ShowNewSectionCommand.Execute("Recents");
			args.Handled = true;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			Assert.IsNotNull(XamlRoot, "XamlRoot was not initialized before measure");
#if HAS_UNO
			Assert.IsTrue(XamlRoot.VisualTree.ContentRoot.CompositionContent.RasterizationScaleInitialized, "Rasterization scale was not initialized");
#endif

			if (_initialMeasure && availableSize == default)
			{
				Assert.Fail("Initial Measure should not be called with empty size");
			}

			_initialMeasure = false;
			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size availableSize)
		{
			if (_initialArrange && availableSize == default)
			{
				Assert.Fail("Initial Arrange should not be called with empty size");
			}

			_initialArrange = false;
			return base.ArrangeOverride(availableSize);
		}

		private void OnSearchEnterKey_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Enter)
			{
				((SampleChooserViewModel)DataContext).TryOpenSingleSearchResult();
			}
		}

		private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
			{
				((SampleChooserViewModel)DataContext).SearchTerm = sender.Text;
			}
		}

		private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			if (args is not null)
			{
				((SampleChooserViewModel)DataContext).TryOpenSingleSearchResult();
			}
		}

		private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			if (args.SelectedItem is SampleChooserContent control)
			{
				_ = ((SampleChooserViewModel)DataContext).OpenSample(CancellationToken.None, control);
			}
		}
	}
}
