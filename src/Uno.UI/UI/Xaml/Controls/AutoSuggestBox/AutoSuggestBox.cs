
using System;
using System.Collections.Specialized;
using System.Linq;
using Uno.Extensions.Specialized;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class AutoSuggestBox : ItemsControl, IValueChangedListener
	{
		private TextBox _textBox;
		private Popup _popup;
		private Grid _layoutRoot;
		private ListView _suggestionsList;
		private Button _queryButton;
		private AutoSuggestionBoxTextChangeReason _textChangeReason = AutoSuggestionBoxTextChangeReason.ProgrammaticChange;
		private string userInput;
		private BindingPath _textBoxBinding;
		private FrameworkElement _suggestionsContainer;

		public AutoSuggestBox() : base()
		{
			Items.VectorChanged += OnItemsChanged;

			DefaultStyleKey = typeof(AutoSuggestBox);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_textBox = GetTemplateChild("TextBox") as TextBox;
			_popup = GetTemplateChild("SuggestionsPopup") as Popup;
			_layoutRoot = GetTemplateChild("LayoutRoot") as Grid;
			_suggestionsList = GetTemplateChild("SuggestionsList") as ListView;
			_suggestionsContainer = GetTemplateChild("SuggestionsContainer") as FrameworkElement;
			_queryButton = GetTemplateChild("QueryButton") as Button;

			// Uno specific: If the user enabled the legacy behavior for popup light dismiss default
			// we force it to false explicitly to make sure the AutoSuggestBox works correctly.
			if (FeatureConfiguration.Popup.EnableLightDismissByDefault)
			{
				_popup.IsLightDismissEnabled = false;
			}

#if __ANDROID__
			_popup.DisableFocus();
#endif

#if __IOS__
			if (_textBox is { } textbox)
			{
				textbox.IsKeepingFocusOnEndEditing = true;
			}
#endif

			UpdateQueryButton();
			UpdateTextBox();
			UpdateDescriptionVisibility(true);

			_textBoxBinding = new BindingPath("Text", null) { DataContext = _textBox, ValueChangedListener = this };

			Loaded += (s, e) => RegisterEvents();
			Unloaded += (s, e) => UnregisterEvents();

			if (IsLoaded)
			{
				RegisterEvents();
			}
		}

		void IValueChangedListener.OnValueChanged(object value)
		{
			if (value is string str)
			{
				// If TextBox's Text value is null, we ignore it.
				Text = str;
			}
		}

		private void OnItemsChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
		{
			UpdateSuggestionList();
		}

		protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnItemsSourceChanged(e);

			UpdateSuggestionList();
		}

		internal override void OnItemsSourceSingleCollectionChanged(object sender, NotifyCollectionChangedEventArgs args, int section)
		{
			base.OnItemsSourceSingleCollectionChanged(sender, args, section);

			UpdateSuggestionList();
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new ListViewItem() { IsGeneratedContainer = true };
		}

		internal override void OnItemsSourceGroupsChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			base.OnItemsSourceGroupsChanged(sender, args);

			UpdateSuggestionList();
		}

		private void UpdateSuggestionList()
		{
			if (_suggestionsList != null)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug("ItemsChanged, refreshing suggestion list");
				}

				_suggestionsList.ItemsSource = GetItems();

				if (GetItems().Count() == 0)
				{
					IsSuggestionListOpen = false;
				}
				else if (_textBox?.IsFocused ?? false)
				{
					IsSuggestionListOpen = true;
					_suggestionsList.ItemsSource = GetItems();

					LayoutPopup();
				}
			}
		}

		private void UpdateTextFromSuggestion(Object o)
		{
			_textChangeReason = AutoSuggestionBoxTextChangeReason.SuggestionChosen;
			Text = GetObjectText(o) ?? "";
		}

		private void UpdateUserInput(Object o)
		{
			userInput = GetObjectText(o);
		}

		private void LayoutPopup()
		{
			if (_popup is Popup popup &&
				popup.IsOpen &&
				popup.Child is FrameworkElement popupChild &&
				_suggestionsContainer is not null &&
				_layoutRoot is not null)
			{
				// Reset popup offsets (Windows seems to do that)
				popup.VerticalOffset = 0;
				popup.HorizontalOffset = 0;

				// Inject layouting constraints
				popupChild.MinHeight = _layoutRoot.ActualHeight;
				popupChild.MinWidth = _layoutRoot.ActualWidth;
				popupChild.MaxHeight = MaxSuggestionListHeight;

				Rect windowRect;
				if (WinUICoreServices.Instance.InitializationType == Uno.UI.Xaml.Core.InitializationType.IslandsOnly)
				{
					windowRect = XamlRoot.VisualTree.VisibleBounds;
				}
				else
				{
					windowRect = ApplicationView.GetForCurrentView().VisibleBounds;
				}

				var inputPaneRect = InputPane.GetForCurrentView().OccludedRect;

				if (inputPaneRect.Height > 0)
				{
					windowRect.Height -= inputPaneRect.Height;
				}

				var popupTransform = (MatrixTransform)popup.TransformToVisual(XamlRoot.Content);
				var popupRect = new Rect(popupTransform.Matrix.OffsetX, popupTransform.Matrix.OffsetY, popup.ActualWidth, popup.ActualHeight);

				var containerTransform = (MatrixTransform)_layoutRoot.TransformToVisual(XamlRoot.Content);
				var containerRect = new Rect(containerTransform.Matrix.OffsetX, containerTransform.Matrix.OffsetY, _layoutRoot.ActualWidth, _layoutRoot.ActualHeight);
				var textBoxHeight = _layoutRoot.ActualHeight;

				// Because Popup.Child is not part of the visual tree until Popup.IsOpen,
				// some descendant Controls may never have loaded and materialized their templates.
				// We force the materialization of all templates to ensure that Measure works properly.
				foreach (var control in popupChild.EnumerateAllChildren().OfType<Control>())
				{
					control.ApplyTemplate();
				}

				popupChild.Measure(windowRect.Size);
				var popupChildRect = new Rect(new Point(), popupChild.DesiredSize);

				// Align left of popup with left of background 
				double targetX = containerRect.Left;
				if (popupChildRect.Right > windowRect.Right) // popup overflows at right
				{
					// Align right of popup with right of background
					targetX = containerRect.Right - popupChildRect.Width;
				}
				if (popupChildRect.Left < windowRect.Left) // popup overflows at left
				{
					// Align center of popup with center of window
					targetX = (windowRect.Width - popupChildRect.Width) / 2.0;
				}

				// Calculates the maximum available popup height for a given target rect
				double targetY;
				double? targetHeight = null;

				var yBelow = containerRect.Top + textBoxHeight;

				// Try to align popup below TextBox
				var availableHeightBelow = windowRect.Bottom - yBelow;
				var availableHeightAbove = containerRect.Top - windowRect.Top;

				// Find to position the popup in this order:
				//  1. Below text box completely
				//  2. Above text box completely
				//  3. To the position where is more space available
				if (availableHeightBelow >= popupChild.DesiredSize.Height)
				{
					targetY = yBelow;
				}
				else if (availableHeightAbove >= popupChild.DesiredSize.Height)
				{
					targetY = containerRect.Top - popupChild.DesiredSize.Height;
				}
				else if (availableHeightBelow > availableHeightAbove)
				{
					targetY = containerRect.Top + textBoxHeight;
					targetHeight = availableHeightBelow;
				}
				else
				{
					targetY = containerRect.Top - availableHeightAbove;
					targetHeight = availableHeightAbove;
				}

				popup.HorizontalOffset = targetX - popupRect.X;
				popup.VerticalOffset = targetY - popupRect.Y;

				if (targetHeight is double requestedHeight)
				{
					_suggestionsContainer.MaxHeight = requestedHeight;
				}
			}
		}

		void RegisterEvents()
		{
			if (_textBox != null)
			{
				_textBox.KeyDown += OnTextBoxKeyDown;
			}

			if (_queryButton != null)
			{
				_queryButton.Click += OnQueryButtonClick;
			}

			if (_suggestionsList != null)
			{
				_suggestionsList.ItemClick += OnSuggestionListItemClick;
			}

			if (_popup != null)
			{
				_popup.Closed += OnPopupClosed;
			}
		}

		void UnregisterEvents()
		{
			if (_textBox != null)
			{
				_textBox.KeyDown -= OnTextBoxKeyDown;
			}

			if (_queryButton != null)
			{
				_queryButton.Click -= OnQueryButtonClick;
			}

			if (_suggestionsList != null)
			{
				_suggestionsList.ItemClick -= OnSuggestionListItemClick;
			}

			if (_popup != null)
			{
				_popup.Closed -= OnPopupClosed;
			}
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			IsSuggestionListOpen = false;
		}

		private void OnPopupClosed(object sender, object e)
		{
			IsSuggestionListOpen = false;
		}

		private void OnIsSuggestionListOpenChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is bool isOpened && _popup != null)
			{
				_popup.IsOpen = isOpened;
			}
		}

		private void UpdateQueryButton()
		{
			if (_queryButton == null)
			{
				return;
			}

			_queryButton.Content = QueryIcon;
			_queryButton.Visibility = QueryIcon == null ? Visibility.Collapsed : Visibility.Visible;
		}

		private void UpdateTextBox()
		{
			if (_textBox == null)
			{
				return;
			}

			_textBox.Text = Text;
		}

		private void OnSuggestionListItemClick(object sender, ItemClickEventArgs e)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Suggestion item clicked {e.ClickedItem}");
			}

			ChoseItem(e.ClickedItem);
			SubmitSearch(e.ClickedItem);
		}

		private void OnQueryButtonClick(object sender, RoutedEventArgs e)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Query button clicked");
			}

			SubmitSearch(null);
		}

		private void SubmitSearch(object item)
		{
			QuerySubmitted?.Invoke(this, new AutoSuggestBoxQuerySubmittedEventArgs(item, _textBox.Text));

			IsSuggestionListOpen = false;
		}

		private void OnTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Enter)
			{
				e.Handled = true;
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Enter key pressed");
				}

				SubmitSearch(IsSuggestionListOpen ? _suggestionsList.SelectedItem : null);
			}
			else if ((e.Key == Windows.System.VirtualKey.Up || e.Key == Windows.System.VirtualKey.Down) && IsSuggestionListOpen)
			{
				e.Handled = true;
				HandleUpDownKeys(e);
			}
			else if (e.Key == Windows.System.VirtualKey.Escape && IsSuggestionListOpen)
			{
				e.Handled = true;
				RevertTextToUserInput();
				IsSuggestionListOpen = false;
			}
			else
			{
				_textChangeReason = AutoSuggestionBoxTextChangeReason.UserInput;
			}
		}

		private void HandleUpDownKeys(KeyRoutedEventArgs e)
		{
			int currentIndex = _suggestionsList.SelectedIndex;
			int numSuggestions = _suggestionsList.NumberOfItems;
			int nextIndex = -1;

			if (e.Key == Windows.System.VirtualKey.Up)
			{
				// C# modulo isn't actually a modulo it's a remainder, so need to account for negative index
				nextIndex = ((currentIndex % numSuggestions) + numSuggestions) % numSuggestions - ((currentIndex == -1) ? 0 : 1);
			}
			else if (e.Key == Windows.System.VirtualKey.Down)
			{
				int indexPlusOne = currentIndex + 1;
				// The next step after the last index should be -1, not 0.
				nextIndex = ((indexPlusOne % numSuggestions) + numSuggestions) % numSuggestions - ((indexPlusOne == numSuggestions) ? 1 : 0);
			}

			_suggestionsList.SelectedIndex = nextIndex;

			if (nextIndex == -1)
			{
				RevertTextToUserInput();
			}
			else
			{
				ChoseSuggestion();
			}
		}

		private void ChoseSuggestion()
		{
			ChoseItem(_suggestionsList.SelectedItem);
		}

		internal void ChoseItem(Object o)
		{
			if (UpdateTextOnSelect)
			{
				UpdateTextFromSuggestion(o);
			}

			SuggestionChosen?.Invoke(this, new AutoSuggestBoxSuggestionChosenEventArgs(o));

			_textBox?.Select(_textBox.Text.Length, 0);
		}

		private void RevertTextToUserInput()
		{
			_suggestionsList.SelectedIndex = -1;
			_textChangeReason = AutoSuggestionBoxTextChangeReason.ProgrammaticChange;

			Text = userInput ?? "";
		}

		private string GetObjectText(Object o)
		{
			if (o is string s)
			{
				return s;
			}

			var value = o;

			if (TextMemberPath != null)
			{
				using var bindingPath = new BindingPath(TextMemberPath, "", null, allowPrivateMembers: true) { DataContext = o };
				value = bindingPath.Value;
			}

			return value?.ToString() ?? "";
		}

		private static void OnTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var newValue = args.NewValue as string ?? string.Empty;

			if (dependencyObject is AutoSuggestBox tb)
			{
				// On some platforms, the TextChangeReason is not updated
				// as KeyDown is not triggered (e.g. Android)
				if (tb._textChangeReason != AutoSuggestionBoxTextChangeReason.SuggestionChosen && tb._textBox is not null)
				{
					if (tb._textBox.IsUserModifying)
					{
						tb._textChangeReason = AutoSuggestionBoxTextChangeReason.UserInput;
					}
					else
					{
						tb._textChangeReason = AutoSuggestionBoxTextChangeReason.ProgrammaticChange;
					}
				}

				tb.UpdateTextBox();
				tb.UpdateSuggestionList();

				if (tb._textChangeReason == AutoSuggestionBoxTextChangeReason.UserInput)
				{
					tb.UpdateUserInput(newValue);
				}

				tb.TextChanged?.Invoke(tb, new AutoSuggestBoxTextChangedEventArgs()
				{
					Reason = tb._textChangeReason,
					Owner = tb
				});

				// Reset the default - otherwise SuggestionChosen could remain set.
				tb._textChangeReason = AutoSuggestionBoxTextChangeReason.ProgrammaticChange;
			}
		}

		private void UpdateDescriptionVisibility(bool initialization)
		{
			if (initialization && Description == null)
			{
				// Avoid loading DescriptionPresenter element in template if not needed.
				return;
			}

			var descriptionPresenter = this.FindName("DescriptionPresenter") as ContentPresenter;
			if (descriptionPresenter != null)
			{
				descriptionPresenter.Visibility = Description != null ? Visibility.Visible : Visibility.Collapsed;
			}
		}
	}
}
