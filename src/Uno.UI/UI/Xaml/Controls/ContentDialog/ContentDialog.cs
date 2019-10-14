#pragma warning disable 67

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Client;
using Uno.Disposables;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentDialog : ContentControl
	{
		internal readonly Popup _popup;
		private TaskCompletionSource<ContentDialogResult> _tcs;
		private readonly SerialDisposable _subscriptions = new SerialDisposable();

		public ContentDialog() : base()
		{
			_popup = new Popup();
			_popup.PopupPanel = new ContentDialogPopupPanel(this);
			_popup.Opened += (s, e) =>
			{
				Opened?.Invoke(this, new ContentDialogOpenedEventArgs());
				VisualStateManager.GoToState(this, "DialogShowing", true);
			};
			this.KeyDown += OnPopupKeyDown;

			Loaded += (s, e) => RegisterEvents();
			Unloaded += (s, e) => UnregisterEvents();
		}

		private void OnPopupKeyDown(object sender, KeyRoutedEventArgs e)
		{
			switch (e.Key)
			{
				case System.VirtualKey.Enter:
					switch (DefaultButton)
					{
						case ContentDialogButton.Close:
							ProcessCloseButton();
							break;

						case ContentDialogButton.Primary:
							ProcessPrimaryButton();
							break;

						case ContentDialogButton.Secondary:
							ProcessSecondaryButton();
							break;

						default:
						case ContentDialogButton.None:
							break;
					}
					break;

				case System.VirtualKey.Escape:
					ProcessCloseButton();
					break;
			}
		}

		public void Hide() => Hide(ContentDialogResult.None);
		private void Hide(ContentDialogResult result)
		{
			void Complete(ContentDialogClosingEventArgs args)
			{
				if (!args.Cancel)
				{
					_popup.IsOpen = false;

					Closed?.Invoke(this, new ContentDialogClosedEventArgs(result));
				}
			}
			var closingArgs = new ContentDialogClosingEventArgs(Complete, result);

			Closing?.Invoke(this, closingArgs);

			if (!closingArgs.IsDeferred)
			{
				Complete(closingArgs);
			}
			else
			{
				closingArgs.EventRaiseCompleted();
			}
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UpdateButtonsVisualStates();
			ApplyDefaultButtonChanged(DefaultButton);
		}

		public IAsyncOperation<ContentDialogResult> ShowAsync()
			=> AsyncOperation.FromTask(async ct =>
			{
				if (_popup.IsOpen)
				{
					throw new InvalidOperationException("A ContentDialog is already opened.");
				}

				_popup.Child = this;

				_popup.IsOpen = true;
				_popup.IsLightDismissEnabled = false;

				_tcs = new TaskCompletionSource<ContentDialogResult>();

				return await _tcs.Task;
			});

		public event TypedEventHandler<ContentDialog, ContentDialogClosedEventArgs> Closed;

		public event TypedEventHandler<ContentDialog, ContentDialogClosingEventArgs> Closing;

		public event TypedEventHandler<ContentDialog, ContentDialogOpenedEventArgs> Opened;

		public event TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> PrimaryButtonClick;

		public event TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> SecondaryButtonClick;

		public event TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> CloseButtonClick;

		private void UpdateButtonsVisualStates()
		{
			var primaryVisible = !string.IsNullOrEmpty(PrimaryButtonText);
			var secondaryVisible = !string.IsNullOrEmpty(SecondaryButtonText);
			var closeVisible = !string.IsNullOrEmpty(CloseButtonText);

			string getState()
			{
				if (primaryVisible && secondaryVisible && closeVisible)
				{
					return "AllVisible";
				}
				else if (!primaryVisible && !secondaryVisible && !closeVisible)
				{
					return "NoneVisible";
				}
				else if (primaryVisible && !secondaryVisible && !closeVisible)
				{
					return "PrimaryVisible";
				}
				else if (!primaryVisible && secondaryVisible && !closeVisible)
				{
					return "SecondaryVisible";
				}
				else if (!primaryVisible && !secondaryVisible && closeVisible)
				{
					return "CloseVisible";
				}
				else if (primaryVisible && secondaryVisible && !closeVisible)
				{
					return "PrimaryAndSecondaryVisible";
				}
				else if (primaryVisible && !secondaryVisible && closeVisible)
				{
					return "PrimaryAndCloseVisible";
				}
				else if (!primaryVisible && secondaryVisible && closeVisible)
				{
					return "SecondaryAndCloseVisible";
				}

				return "none";
			}
			var state = getState();
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().LogDebug($"SetState {state}");
			}
			VisualStateManager.GoToState(this, state, true);
		}

		private void OnPrimaryButtonTextChanged(string oldValue, string newValue)
			=> UpdateButtonsVisualStates();

		private void OnSecondayButtonTextChanged(string oldValue, string newValue)
			=> UpdateButtonsVisualStates();

		private void OnCloseButtonTextChanged(string oldValue, string newValue)
			=> UpdateButtonsVisualStates();

		public IAsyncOperation<ContentDialogResult> ShowAsync(ContentDialogPlacement placement)
			=> ShowAsync();

		private void UnregisterEvents()
		{
			_subscriptions.Disposable = null;
		}

		private void RegisterEvents()
		{
			_subscriptions.Disposable = null;

			var d = new CompositeDisposable();

			if (GetTemplateChild("PrimaryButton") is Button primaryButton)
			{
				primaryButton.Click += OnPrimaryButtonClicked;

				d.Add(() =>
				{
					primaryButton.Click -= OnPrimaryButtonClicked;
				});
			}

			if (GetTemplateChild("SecondaryButton") is Button secondaryButton)
			{
				secondaryButton.Click += OnSecondaryButtonClicked;

				d.Add(() =>
				{
					secondaryButton.Click -= OnSecondaryButtonClicked;
				});
			}

			if (GetTemplateChild("CloseButton") is Button closeButton)
			{
				closeButton.Click += OnCloseButtonClicked;

				d.Add(() =>
				{
					closeButton.Click -= OnCloseButtonClicked;
				});
			}

			_subscriptions.Disposable = d;
		}

		private void OnCloseButtonClicked(object sender, RoutedEventArgs e)
			=> ProcessCloseButton();

		private void OnSecondaryButtonClicked(object sender, RoutedEventArgs e)
			=> ProcessSecondaryButton();

		private void OnPrimaryButtonClicked(object sender, RoutedEventArgs e)
			=> ProcessPrimaryButton();

		private void ProcessCloseButton()
		{
			void Complete(ContentDialogButtonClickEventArgs a)
			{
				if (!a.Cancel)
				{
					const ContentDialogResult result = ContentDialogResult.None;
					_tcs.SetResult(result);
					CloseButtonCommand.ExecuteIfPossible(CloseButtonCommandParameter);
					Hide(result);
				}
			}

			var args = new ContentDialogButtonClickEventArgs(Complete);
			CloseButtonClick?.Invoke(this, args);

			if (args.Deferral == null)
			{
				Complete(args);
			}
		}

		private void ProcessSecondaryButton()
		{
			void Complete(ContentDialogButtonClickEventArgs a)
			{
				if (!a.Cancel)
				{
					const ContentDialogResult result = ContentDialogResult.Secondary;
					_tcs.SetResult(result);
					SecondaryButtonCommand.ExecuteIfPossible(SecondaryButtonCommandParameter);
					Hide(result);
				}
			}

			var args = new ContentDialogButtonClickEventArgs(Complete);
			SecondaryButtonClick?.Invoke(this, args);

			if (args.Deferral == null)
			{
				Complete(args);
			}
		}


		private void ProcessPrimaryButton()
		{
			void Complete(ContentDialogButtonClickEventArgs a)
			{
				if (!a.Cancel)
				{
					const ContentDialogResult result = ContentDialogResult.Primary;
					_tcs.SetResult(result);
					PrimaryButtonCommand.ExecuteIfPossible(PrimaryButtonCommandParameter);

					Hide(result);
				}
			}

			var args = new ContentDialogButtonClickEventArgs(Complete);
			PrimaryButtonClick?.Invoke(this, args);

			if (args.Deferral == null)
			{
				Complete(args);
			}
		}

		// Override the default style resolution, as ContentDialog
		// is almost always overridden when defined in XAML.
		internal override Type GetDefaultStyleType()
			=> typeof(ContentDialog);

		private void OnDefaultButtonChanged(ContentDialogButton oldValue, ContentDialogButton newValue)
		{
			ApplyDefaultButtonChanged(newValue);
		}

		private void ApplyDefaultButtonChanged(ContentDialogButton newValue)
		{
			switch (newValue)
			{
				case ContentDialogButton.None:
					VisualStateManager.GoToState(this, "NoDefaultButton", true);
					break;

				case ContentDialogButton.Close:
				case ContentDialogButton.Primary:
				case ContentDialogButton.Secondary:
					VisualStateManager.GoToState(this, $"{newValue}AsDefaultButton", true);
					break;
			}
		}
	}
}
