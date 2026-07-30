#nullable enable

using Microsoft.UI.Dispatching;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class SemanticZoom
{
	private readonly SerialDisposable m_templateSubscriptions = new();
	private readonly DispatcherQueueTimer m_zoomOutButtonHideTimer;

	private SemanticZoomViewChangedEventArgs? m_tpCompletedArgs;
	private ISemanticZoomInformation? m_tpSourceView;
	private ISemanticZoomInformation? m_tpDestinationView;
	private ContentPresenter? m_tpSourcePresenter;
	private ContentPresenter? m_tpDestinationPresenter;
}
