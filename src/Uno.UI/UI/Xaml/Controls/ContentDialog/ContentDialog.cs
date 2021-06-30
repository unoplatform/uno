#pragma warning disable 67

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Client;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.System;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.ViewManagement;

namespace Windows.UI.Xaml.Controls
{
	public partial class
		ContentDialog : ContentControl
	{
		internal readonly Popup _popup;
		private TaskCompletionSource<ContentDialogResult> _tcs;
		private readonly SerialDisposable _subscriptions = new SerialDisposable();
		private bool _hiding = false;

		private Border m_tpBackgroundElementPart;
		private Border m_tpButton1HostPart;
		private Border m_tpButton2HostPart;
		private ButtonBase m_tpCloseButtonPart;
		private Grid m_tpCommandSpacePart;
		private Border m_tpContainerPart;
		private Grid m_tpContentPanelPart;
		private ContentPresenter m_tpContentPart;
		private ScrollViewer m_tpContentScrollViewerPart;
		private Grid m_tpDialogSpacePart;
		private Grid m_tpLayoutRootPart;
		private ButtonBase m_tpPrimaryButtonPart;
		private ScaleTransform m_tpScaleTransformPart;
		private ButtonBase m_tpSecondaryButtonPart;
		private ContentControl m_tpTitlePart;

		public ContentDialog() : base()
		{
			_popup = new Popup()
			{
				LightDismissOverlayMode = LightDismissOverlayMode.On,
			};

			ResourceResolver.ApplyResource(_popup, Popup.LightDismissOverlayBackgroundProperty, "ContentDialogLightDismissOverlayBackground", isThemeResourceExtension: true);

			_popup.PopupPanel = new ContentDialogPopupPanel(this);

			var thisRef = (this as IWeakReferenceProvider).WeakReference;
			_popup.Opened += (s, e) =>
			{
				if (thisRef.Target is ContentDialog that)
				{
					that.Opened?.Invoke(that, new ContentDialogOpenedEventArgs());
					that.UpdateVisualState();
				}
			};
			this.KeyDown += OnPopupKeyDown;
			var inputPane = InputPane.GetForCurrentView();
			inputPane.Showing += (o, e) =>
			  {
				  if (thisRef.Target is ContentDialog that)
				  {
					  that.UpdateVisualState();
				  }
			  };
			inputPane.Hiding += (o, e) =>
			{
				if (thisRef.Target is ContentDialog that)
				{
					that.UpdateVisualState();
				}
			};

			Loaded += (s, e) => RegisterEvents();
			Unloaded += (s, e) => UnregisterEvents();

			DefaultStyleKey = typeof(ContentDialog);
		}

		private void OnPopupKeyDown(object sender, KeyRoutedEventArgs e)
		{
			switch (e.Key)
			{
				case VirtualKey.Enter:
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

				case VirtualKey.Escape:
					ProcessCloseButton();
					break;
			}
		}

		public void Hide()
		{
			if (_hiding)
			{
				return;
			}
			_hiding = true;
			Hide(ContentDialogResult.None);
		}

		private bool Hide(ContentDialogResult result)
		{
			void Complete(ContentDialogClosingEventArgs args)
			{
				if (!args.Cancel)
				{
					m_isShowing = false;
					_popup.IsOpen = false;
					_popup.Child = null;
					UpdateVisualState();
					Closed?.Invoke(this, new ContentDialogClosedEventArgs(result));
					_tcs.SetResult(result);
				}
				_hiding = false;
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

			return !closingArgs.Cancel;
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			GetTemplateParts();

			m_dialogMinHeight = ResourceResolver.ResolveTopLevelResourceDouble("ContentDialogMinHeight");

			// For dialogs that were shown when not in the visual tree, since we couldn't prepare
			// their content during the ShowAsync() call, do it now that it's loaded.
			if (m_placementMode == PlacementMode.EntireControlInPopup)
			{
				PrepareContent();
			}

			// UNO TODO
			//IFC_RETURN(m_epLayoutRootGotFocusHandler.AttachEventHandler(
			//m_tpLayoutRootPart.AsOrNull<IUIElement>().Get(),

			//[this](IInspectable *, xaml::IRoutedEventArgs *)

			//{
			//	// Update which command button has the default button visualization.
			//	return UpdateVisualState();
			//}));
		}

		void GetTemplateParts()
		{
			m_tpBackgroundElementPart = GetTemplateChild("BackgroundElement") as Border;
			m_tpButton1HostPart = GetTemplateChild("Button1Host") as Border;
			m_tpButton2HostPart = GetTemplateChild("Button2Host") as Border;
			m_tpCloseButtonPart = GetTemplateChild("CloseButton") as ButtonBase;
			m_tpCommandSpacePart = GetTemplateChild("CommandSpace") as Grid;
			m_tpContainerPart = GetTemplateChild("Container") as Border;
			m_tpContentPanelPart = GetTemplateChild("ContentPanel") as Grid;
			m_tpContentPart = GetTemplateChild("Content") as ContentPresenter;
			m_tpContentScrollViewerPart = GetTemplateChild("ContentScrollViewer") as ScrollViewer;
			m_tpDialogSpacePart = GetTemplateChild("DialogSpace") as Grid;
			m_tpLayoutRootPart = GetTemplateChild("LayoutRoot") as Grid;
			m_tpPrimaryButtonPart = GetTemplateChild("PrimaryButton") as ButtonBase;
			m_tpScaleTransformPart = GetTemplateChild("ScaleTransform") as ScaleTransform;
			m_tpSecondaryButtonPart = GetTemplateChild("SecondaryButton") as ButtonBase;
			m_tpTitlePart = GetTemplateChild("Title") as ContentControl;
		}

		private bool HasValidAppliedTemplate() => m_tpBackgroundElementPart != null; // Uno note: stand-in for m_templateVersion != TemplateVersion::Unsupported, which is used in this sense

		public IAsyncOperation<ContentDialogResult> ShowAsync()
			=> AsyncOperation.FromTask(async ct =>
			{
				if (_popup.IsOpen)
				{
					throw new InvalidOperationException("A ContentDialog is already opened.");
				}

				// TODO: support in-place
				m_placementMode = PlacementMode.EntireControlInPopup;

				// Make sure default template is applied, so visual states etc can be set correctly
				EnsureTemplate();
				if (HasValidAppliedTemplate())
				{
					PrepareContent();
				}

				_popup.Child = this;

				m_isShowing = true;
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

					CloseButtonCommand.ExecuteIfPossible(CloseButtonCommandParameter);

					Hide(result);
				}
				else
				{
					_hiding = false;
				}
			}

			if (_hiding)
			{
				return;
			}
			_hiding = true;

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
					SecondaryButtonCommand.ExecuteIfPossible(SecondaryButtonCommandParameter);
					Hide(result);
				}
				else
				{
					_hiding = false;
				}
			}

			if (_hiding)
			{
				return;
			}
			_hiding = true;

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
					PrimaryButtonCommand.ExecuteIfPossible(PrimaryButtonCommandParameter);

					Hide(result);
				}
				else
				{
					_hiding = false;
				}
			}

			if (_hiding)
			{
				return;
			}
			_hiding = true;

			var args = new ContentDialogButtonClickEventArgs(Complete);
			PrimaryButtonClick?.Invoke(this, args);

			if (args.Deferral == null)
			{
				Complete(args);
			}
		}
	}
}
