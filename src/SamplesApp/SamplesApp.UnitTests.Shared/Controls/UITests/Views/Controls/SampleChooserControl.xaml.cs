#nullable enable

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
using System.Threading.Tasks;


#if WINAPPSDK
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
#elif __ANDROID__ || __APPLE_UIKIT__ || UNO_REFERENCE_API
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

			InitializePerfHooks();
			InitializeShortcuts();
			InitializeShell();

			DataContextChanged += OnDataContextChanged;
		}

		/// <summary>
		/// Typed source for x:Bind. Null until MainPage sets the DataContext, so bound functions must be null-safe.
		/// </summary>
		internal SampleChooserViewModel? ViewModel => DataContext as SampleChooserViewModel;

		private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) => Bindings.Update();

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

		private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
			{
				if (ViewModel is { } vm)
				{
					vm.SearchTerm = sender.Text;
				}
			}
		}

		// Suggestions open on submit (click or Enter), not on SuggestionChosen, which also fires while arrowing through them.
		private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			if (ViewModel is not { } vm)
			{
				return;
			}

			switch (args.ChosenSuggestion)
			{
				case SampleChooserContent sample:
					_ = vm.OpenSample(CancellationToken.None, sample);
					CloseOverlayBrowser(vm);
					break;

				case Uno.UI.Samples.Helper.SearchSeeAllItem:
					ShowAllSearchResults(sender, vm);
					break;

				case null when !string.IsNullOrWhiteSpace(sender.Text):
					vm.SearchTerm = sender.Text;
					if (IsControlDown || !vm.TryOpenTopSearchResult())
					{
						ShowAllSearchResults(sender, vm);
					}
					else
					{
						CloseOverlayBrowser(vm);
					}
					break;
			}
		}

		// Focus moves on to the results: WinUI would otherwise reopen the suggestions over them when they refresh.
		private void ShowAllSearchResults(AutoSuggestBox box, SampleChooserViewModel vm)
		{
			// Only a keyboard submit (Enter, Ctrl+Enter) should show the focus rectangle on the results.
			var focusState = IsEnterDown ? FocusState.Keyboard : FocusState.Programmatic;
			vm.ShowSearchResults();
			DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
			{
				box.IsSuggestionListOpen = false;
				ShellSearchResultsList.Focus(focusState);
			});
		}

		private void InfoFlyout_Opening(object sender, object e)
		{
			SampleInfoFlyoutContent.DataContext = ViewModel?.CurrentSelectedSample;
		}
	}
}
