#nullable enable

using Uno.Disposables;
using Windows.Foundation;
using Windows.UI.Composition.Interactions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Private.Controls
{
	internal partial class ScrollViewerIRefreshInfoProviderAdapter
	{
		private RefreshInfoProviderImpl? m_infoProvider = null;
		private IAdapterAnimationHandler m_animationHandler = null;
		private ScrollViewer? m_scrollViewer = null;
		private RefreshPullDirection m_refreshPullDirection = RefreshPullDirection.TopToBottom;
		private InteractionTracker? m_interactionTracker = null;
		private VisualInteractionSource? m_visualInteractionSource = null;
		private bool m_visualInteractionSourceIsAttached = false

		private SerialDisposable m_scrollViewer_LoadedToken = new SerialDisposable();
		private SerialDisposable m_scrollViewer_PointerPressedToken = new SerialDisposable();
		private SerialDisposable m_scrollViewer_DirectManipulationCompletedToken = new SerialDisposable();
		private SerialDisposable m_scrollViewer_ViewChangingToken = new SerialDisposable();
		private SerialDisposable m_infoProvider_RefreshStartedToken = new SerialDisposable();
		private SerialDisposable m_infoProvider_RefreshCompletedToken = new SerialDisposable();

		//tracker_ref<winrt::IInspectable> m_boxedPointerPressedEventHandler { this };
	}
}
