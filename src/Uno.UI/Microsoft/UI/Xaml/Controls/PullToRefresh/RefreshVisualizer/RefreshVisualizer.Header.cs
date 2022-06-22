#nullable enable
#pragma warning disable CS0414
#pragma warning disable CS0169

using Microsoft.UI.Private.Controls;
using Uno.Disposables;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class RefreshVisualizer
{
	//////////////////////////////////////////////////////
	////	         DependencyPropertyBackers      //////
	//////////////////////////////////////////////////////
	private RefreshVisualizerOrientation m_orientation = RefreshVisualizerOrientation.Auto;
	private RefreshVisualizerState m_state = RefreshVisualizerState.Idle;
	private IRefreshInfoProvider? m_refreshInfoProvider = null;
	private UIElement? m_content = null;

	private SerialDisposable m_RefreshInfoProvider_InteractingForRefreshChangedToken = new SerialDisposable();
	private SerialDisposable m_RefreshInfoProvider_InteractionRatioChangedToken = new SerialDisposable();

	///////////////////////////////////////////////
	/////////	Internal Reference Vars   /////////
	///////////////////////////////////////////////
	private bool m_isInteractingForRefresh = false;
	private double m_executionRatio = 0.8;
	private double m_interactionRatio = 0.0;
	private Compositor? m_compositor = null;
	private Panel? m_containerPanel = null;
	private Panel? m_root = null;
	private float m_startingRotationAngle = 0.0f;
	private RefreshPullDirection m_pullDirection = RefreshPullDirection.TopToBottom;
}
