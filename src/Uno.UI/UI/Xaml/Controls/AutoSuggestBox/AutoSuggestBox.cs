
using System;
using System.Linq;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System.Collections.Specialized;

#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Windows.UI.Xaml.Controls
{
	public  partial class AutoSuggestBox : ItemsControl
	{
		private TextBox _textBox;
		private Popup _popup;
		private Grid _layoutRoot;
		private ListView _suggestionsList;
		private Button _queryButton;
		private AutoSuggestionBoxTextChangeReason textChangeReason;
		private string userInput;

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
			_queryButton = GetTemplateChild("QueryButton") as Button;

#if __ANDROID__
			_popup.DisableFocus();
#endif

			if (_queryButton != null)
			{
				_queryButton.Content = new SymbolIcon(Symbol.Find);
			}

			_textBox?.SetBinding(
				TextBox.TextProperty,
				new Binding()
				{
					Path = "Text",
					RelativeSource = RelativeSource.TemplatedParent,
					UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
					Mode = BindingMode.TwoWay
				}
			);

			Loaded += (s, e) => RegisterEvents();
			Unloaded += (s, e) => UnregisterEvents();

			if (IsLoaded)
			{
				RegisterEvents();
			}
		}

		private void OnItemsChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
		{
			UpdateSuggestionList();
		}

		protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			// Calling this method before base.OnItemsSourceChanged() ensures that, in the case of an ObservableCollection, the list
			// subscribes to CollectionChanged before AutoSuggestBox does. This is important for Android because the list needs to
			// notify RecyclerView of collection changes before UpdateSuggestionList() measures it, otherwise we get errors like
			// "Inconsistency detected. Invalid view holder adapter position"
			UpdateSuggestionList();

			base.OnItemsSourceChanged(e);
		}

		internal override void OnItemsSourceSingleCollectionChanged(object sender, NotifyCollectionChangedEventArgs args, int section)
		{
			base.OnItemsSourceSingleCollectionChanged(sender, args, section);

			UpdateSuggestionList();
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
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug("ItemsChanged, refreshing suggestion list");
				}

				_suggestionsList.ItemsSource = GetItems();

				if (GetItems().Count() == 0)
				{
					IsSuggestionListOpen = false;
				}
				else
				{
					IsSuggestionListOpen = true;
					_suggestionsList.ItemsSource = GetItems();

					LayoutPopup();
				}
			}
		}

		private void UpdateTextFromSuggestion(Object o)
		{
			textChangeReason = AutoSuggestionBoxTextChangeReason.SuggestionChosen;
			Text = GetObjectText(o);
		}

		private void UpdateUserInput(Object o)
		{
			userInput = GetObjectText(o);
		}

		private void LayoutPopup()
		{
			if (
				_popup != null
				&& _popup.IsOpen
				&& _popup.Child is FrameworkElement popupChild
			)
			{
				if (_popup is Popup popup)
				{
					if (_layoutRoot is FrameworkElement background)
					{
						// Reset popup offsets (Windows seems to do that)
						popup.VerticalOffset = 0;
						popup.HorizontalOffset = 0;

						// Inject layouting constraints
						popupChild.MinHeight = background.ActualHeight;
						popupChild.MinWidth = background.ActualWidth;
						popupChild.MaxHeight = MaxSuggestionListHeight;

						var windowRect = Xaml.Window.Current.Bounds;

						var popupTransform = (MatrixTransform)popup.TransformToVisual(Xaml.Window.Current.Content);
						var popupRect = new Rect(popupTransform.Matrix.OffsetX, popupTransform.Matrix.OffsetY, popup.ActualWidth, popup.ActualHeight);

						var backgroundTransform = (MatrixTransform)background.TransformToVisual(Xaml.Window.Current.Content);
						var backgroundRect = new Rect(backgroundTransform.Matrix.OffsetX, backgroundTransform.Matrix.OffsetY + background.ActualHeight, background.ActualWidth, background.ActualHeight);

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
						popupChildRect.X = backgroundRect.Left;
						if (popupChildRect.Right > windowRect.Right) // popup overflows at right
						{
							// Align right of popup with right of background
							popupChildRect.X = backgroundRect.Right - popupChildRect.Width;
						}
						if (popupChildRect.Left < windowRect.Left) // popup overflows at left
						{
							// Align center of popup with center of window
							popupChildRect.X = (windowRect.Width - popupChildRect.Width) / 2.0;
						}

						// Align top of popup with top of background
						popupChildRect.Y = backgroundRect.Top;
						if (popupChildRect.Bottom > windowRect.Bottom) // popup overflows at bottom
						{
							// Align bottom of popup with bottom of background
							popupChildRect.Y = backgroundRect.Bottom - popupChildRect.Height;
						}
						if (popupChildRect.Top < windowRect.Top) // popup overflows at top
						{
							// Align center of popup with center of window
							popupChildRect.Y = (windowRect.Height - popupChildRect.Height) / 2.0;
						}

						popup.HorizontalOffset = popupChildRect.X - popupRect.X;
						popup.VerticalOffset = popupChildRect.Y - popupRect.Y;
					}
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

		private void OnSuggestionListItemClick(object sender, ItemClickEventArgs e)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Suggestion item clicked {e.ClickedItem}");
			}

			ChoseItem(e.ClickedItem);
			SubmitSearch();
		}

		private void OnQueryButtonClick(object sender, RoutedEventArgs e)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Query button clicked");
			}

			SubmitSearch();
		}

		private void SubmitSearch()
		{
			QuerySubmitted?.Invoke(this, new AutoSuggestBoxQuerySubmittedEventArgs(null, Text));
			IsSuggestionListOpen = false;
		}

		private void OnTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Enter)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Enter key pressed");
				}

				SubmitSearch();
			}
			else if ((e.Key == Windows.System.VirtualKey.Up || e.Key == Windows.System.VirtualKey.Down) && IsSuggestionListOpen)
			{
				HandleUpDownKeys(e);
			}
			else if (e.Key == Windows.System.VirtualKey.Escape && IsSuggestionListOpen)
			{
				RevertTextToUserInput();
				IsSuggestionListOpen = false;
			} else
			{
				textChangeReason = AutoSuggestionBoxTextChangeReason.UserInput;
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
			} else
			{
				ChoseSuggestion();
			}
		}

		private void ChoseSuggestion()
		{
			ChoseItem(_suggestionsList.SelectedItem);
		}

		private void ChoseItem(Object o)
		{
			if (UpdateTextOnSelect)
			{
				UpdateTextFromSuggestion(o);
			}

			SuggestionChosen?.Invoke(this, new AutoSuggestBoxSuggestionChosenEventArgs(o));
		}

		private void RevertTextToUserInput()
		{
			_suggestionsList.SelectedIndex = -1;
			textChangeReason = AutoSuggestionBoxTextChangeReason.ProgrammaticChange;
			Text = userInput;
		}

		private string GetObjectText(Object o)
		{
			string text = "";
			if (TextMemberPath != null)
			{
				text = (string)o.GetValue(TextMemberPathProperty);
			}
			else
			{
				text = o.ToString();
			}
			return text;
		}

		private static void OnTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if(dependencyObject is AutoSuggestBox tb)
			{
				if (tb.textChangeReason == AutoSuggestionBoxTextChangeReason.UserInput)
				{
					tb.UpdateUserInput(args.NewValue);
				}

				tb.TextChanged?.Invoke(tb, new AutoSuggestBoxTextChangedEventArgs() { Reason = tb.textChangeReason });
			}
		}

	}
}
