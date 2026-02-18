// MUX Reference ContentDialog_Partial.h, tag winui3/release/1.6-stable

using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.Helpers;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentDialog
{
	private static readonly double ContentDialog_SIP_Bottom_Margin = 12.0;

	// When ShowAsync is called we determine if the control is
	// currently in the visual tree. This causes the layout to
	// occur differently.
	private enum PlacementMode
	{
		Undetermined,
		EntireControlInPopup,
		TransplantedRootInPopup,
		InPlace,
	}

	private enum FullMode
	{
		Undetermined,
		Partial,
		Full
	}

	/* Template parts */
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

	/* Tracked references */
	private IAsyncOperation<ContentDialogResult> m_tpCurrentAsyncOperation;
	private Popup m_tpPopup;
	private Popup m_tpSmokeLayerPopup;
	private FrameworkElement m_tpSmokeLayer;
	private VisualStateGroup m_tpDialogShowingStates;

	/* Deferral managers */
	private DeferralManager<ContentDialogClosingDeferral> m_spClosingDeferralManager;
	private DeferralManager<ContentDialogButtonClickDeferral> m_spButtonClickDeferralManager;

	/* Revokers */
	private readonly SerialDisposable m_epPrimaryButtonClickHandler = new SerialDisposable();
	private readonly SerialDisposable m_epSecondaryButtonClickHandler = new SerialDisposable();
	private readonly SerialDisposable m_epCloseButtonClickHandler = new SerialDisposable();
	private readonly SerialDisposable m_epLayoutRootLoadedHandler = new SerialDisposable();
	private readonly SerialDisposable m_epLayoutRootKeyDownHandler = new SerialDisposable();
	private readonly SerialDisposable m_epLayoutRootKeyUpHandler = new SerialDisposable();
	private readonly SerialDisposable m_epLayoutRootGotFocusHandler = new SerialDisposable();
	private readonly SerialDisposable m_epLayoutRootProcessKeyboardAcceleratorsHandler = new SerialDisposable();
	private readonly SerialDisposable m_xamlRootChangedEventHandler = new SerialDisposable();
	private readonly SerialDisposable m_popupOpenedHandler = new SerialDisposable();
	private readonly SerialDisposable m_popupChildUnloadedEventHandler = new SerialDisposable();
	private readonly SerialDisposable m_dialogSizeChangedHandler = new SerialDisposable();
	private readonly SerialDisposable m_dialogShowingStateChangedEventHandler = new SerialDisposable();

	/* State */
	private WeakReference m_spFocusedElementBeforeContentDialogShows;

	private bool m_isTemplateApplied;
	private PlacementMode m_placementMode = PlacementMode.Undetermined;
	private bool m_isLayoutRootTransplanted;
	private bool m_hideInProgress;
	private bool m_hasPreparedContent;
	private bool m_prepareSmokeLayerAndPopup;

	// Set once we've received the Popup.Opened event. If the async operation is
	// canceled before this point, we won't fire our Opening, Closing, and ClosedEvents.
	private bool m_isShowing;

	private bool m_isProcessingKeyboardAccelerators;

	// Flag to indicate that we shouldn't fire the closing event when hiding because we
	// want to hide without the ability to cancel it.
	private bool m_skipClosingEventOnHide;

	// True when m_tpSmokeLayer is a control template part (a FrameworkElement). False when it is a Rectangle created in code-behind.
	private bool m_isSmokeLayerATemplatePart;

#if HAS_UNO
	// Uno specific: storyboard for input pane adjustments
	private Storyboard m_layoutAdjustmentsForInputPaneStoryboard;

	// Uno specific: minimum height resolved from resources
	private double m_dialogMinHeight;

	// Uno specific: TaskCompletionSource backing ShowAsync
	private TaskCompletionSource<ContentDialogResult> m_tcs;
#endif
}
