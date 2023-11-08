using System;
using Uno.Disposables;
using Uno.Helpers;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

namespace Windows.UI.Xaml.Controls;

partial class ContentDialog
{
	//	class ContentDialogButtonClickDeferral;
	//	class ContentDialogClosingDeferral;

	//	extern __declspec(selectany)  char ContentDialogShowAsyncOperationName[] = "Windows.Foundation.IAsyncOperation`1<Microsoft.UI.Xaml.Controls.ContentDialogResult> Microsoft.UI.Xaml.Controls.ContentDialog.ShowAsync";

	internal class ContentDialogShowAsyncOperation : IAsyncOperation<ContentDialogResult>
	{
		public AsyncOperationCompletedHandler<ContentDialogResult> Completed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public Exception ErrorCode => throw new NotImplementedException();

		public uint Id => throw new NotImplementedException();

		public AsyncStatus Status => throw new NotImplementedException();

		public void Cancel() => throw new NotImplementedException();
		public void Close() => throw new NotImplementedException();
		public ContentDialogResult GetResults() => throw new NotImplementedException();
	}

	//		public DXamlAsyncBaseImpl<
	//		wf.IAsyncOperationCompletedHandler<ContentDialogResult>,
	//		wf.IAsyncOperation<ContentDialogResult>,
	//		Microsoft.WRL.AsyncCausalityOptions<ContentDialogShowAsyncOperationName>>
	//    {
	//        InspectableClass(wf.IAsyncOperation<ContentDialogResult>.z_get_rc_name_impl(), BaseTrust);

	//	public:
	//        ContentDialogShowAsyncOperation()
	//			: m_result(ContentDialogResult.ContentDialogResult_None)
	//	{ }

	//	IFACEMETHOD(put_Completed)(wf.IAsyncOperationCompletedHandler<ContentDialogResult>* pCompletedHandler) override
	//        {
	//            return __super.PutOnComplete(pCompletedHandler);
	//        }

	//IFACEMETHOD(get_Completed)(out result_maybenull_ wf.IAsyncOperationCompletedHandler<ContentDialogResult> **ppCompletedHandler) override
	//        {
	//            return __super.GetOnComplete(ppCompletedHandler);
	//        }

	//        IFACEMETHOD(Cancel)() override
	//{

	//	__super.Cancel();

	//	if (m_spOwner)
	//	{
	//		m_spOwner.Hide();
	//	}

	//Cleanup:
	//	RRETURN(hr);
	//}

	//private void StartOperation(IContentDialog* pOwner)
	//{


	//	MUX_ASSERT(!m_spOwner, "StartOperation should never be called on an operation that already has an owner.");
	//	m_spOwner = pOwner;
	//	DXamlAsyncBaseImpl.StartOperation();

	//Cleanup:
	//	RRETURN(hr);
	//}

	private void SetResults(ContentDialogResult result)
	{
		m_result = result;
	}

	//IFACEMETHOD(GetResults)(out ContentDialogResult* pResult)

	//		{
	//	*pResult = m_result;
	//	RRETURN(S_OK);
	//}

	//void CoreFireCompletion()
	//{
	//	m_spOwner.Reset();
	//	CoreFireCompletionImpl();
	//}

	private ContentDialogResult m_result;
	
	private double GetDialogHeight()
	{

		double backgroundHeight = 0.0;

		if (m_tpBackgroundElementPart is not null)
		{
			FrameworkElement spBackgroundAsFE;
			m_tpBackgroundElementPart.As(&spBackgroundAsFE);
			backgroundHeight = spBackgroundAsFE.ActualHeight;
		}

		*value = backgroundHeight;
	}

	// When ShowAsync is called we determine if the control is
	// currently in the visual tree. This causes the layout to
	// occur differently.
	private enum PlacementMode
	{
		Undetermined,
		EntireControlInPopup,
		TransplantedRootInPopup,
		InPlace
	}

	private enum FullMode
	{
		Undetermined,
		Partial,
		Full
	}

	private enum TemplateVersion
	{
		Unsupported = 0,
		PhoneBlue = 1,
		Threshold = 2,
		Redstone2 = 3,
		Redstone3 = 4
	}

	private IAsyncOperation<ContentDialogResult> m_tpCurrentAsyncOperation;
	private Popup m_tpPopup;
	private Popup m_tpSmokeLayerPopup;
	private Rectangle m_tpSmokeLayer;
	private VisualStateGroup m_tpDialogShowingStates;

	// TODO:MZ: Deferral manager in Uno is different from WinUI!
	private DeferralFactoryManager<ContentDialogClosingDeferral> m_spClosingDeferralManager;
	private DeferralFactoryManager<ContentDialogButtonClickDeferral> m_spButtonClickDeferralManager;

	private readonly SerialDisposable m_epPrimaryButtonClickHandler = new();
	private readonly SerialDisposable m_epSecondaryButtonClickHandler = new();
	private readonly SerialDisposable m_epCloseButtonClickHandler = new();
	private readonly SerialDisposable m_epWindowActivatedHandler = new();
	private readonly SerialDisposable m_epLayoutRootPointerReleasedHandler = new();
	private readonly SerialDisposable m_epLayoutRootLoadedHandler = new();
	private readonly SerialDisposable m_epLayoutRootKeyDownHandler = new();
	private readonly SerialDisposable m_epLayoutRootKeyUpHandler = new();
	private readonly SerialDisposable m_epLayoutRootGotFocusHandler = new();
	private readonly SerialDisposable m_epLayoutRootProcessKeyboardAcceleratorsHandler = new();
	private readonly SerialDisposable m_epOrientationChangedHandler = new();
	private readonly SerialDisposable m_epWindowSizeChangedHandler = new();
	private readonly SerialDisposable m_popupOpenedHandler = new();
	private readonly SerialDisposable m_popupChildUnloadedEventHandler = new();
	private readonly SerialDisposable m_dialogSizeChangedHandler = new();
	private readonly SerialDisposable m_dialogShowingStateChangedEventHandler = new();

	private ManagedWeakReference m_spFocusedElementBeforeContentDialogShows;

	private TemplateVersion m_templateVersion = TemplateVersion.Unsupported;

	private PlacementMode m_placementMode = PlacementMode.Undetermined;
	private bool m_isLayoutRootTransplanted;
	private bool m_hideInProgress;
	private bool m_hasPreparedContent;
	private double m_preShowStatusBarOpacity;

	// Set once we've received the Popup.Opened event. If the async operation is
	// canceled before this point, we won't fire our Opening, Closing, and ClosedEvents.
	private bool m_isShowing;

	private bool m_isProcessingKeyboardAccelerators;

	//static ULONG z_ulUniqueAsyncActionId;
	// Flag to indicate that we shouldn't fine the closing event when hiding because we
	// want to hide without the ability to cancel it.
	private bool m_skipClosingEventOnHide;

	// Apply our layout adjustments using a storyboard so that we don't stomp over template or user
	// provided values.  When we stop the storyboard, it will restore the previous values.
	private Storyboard m_layoutAdjustmentsForInputPaneStoryboard;

	private double m_dialogMinHeight;

	// This only gets set from the SimulateRegionsForContentDialog() test hook.
	private bool m_simulateRegions;

	// From dxaml\xcp\dxaml\lib\winrtgeneratedclasses\ContentDialog.g.h
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
}
