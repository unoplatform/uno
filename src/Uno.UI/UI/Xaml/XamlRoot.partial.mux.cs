using System.Linq;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Microsoft.UI.Xaml;

partial class XamlRoot
{
	private Rect m_simulatedInputPaneOccludedRect;

	internal Rect SimulatedInputPaneOccludedRect
	{
		set
		{
			m_simulatedInputPaneOccludedRect = value;

			// TODO:MZ:
			//// notify the root scroll viewer of the change
			//if (m_simulatedInputPaneOccludedRect.Width != 0 && m_simulatedInputPaneOccludedRect.Height != 0)
			//{
			//	DXamlCore::GetCurrent()->ClientToScreen(&m_simulatedInputPaneOccludedRect);
			//}

			//var scrollContentControl = VisualTree.GetRootScrollViewer();
			//if (!scrollContentControl)
			//{
			//	// nothing further to do
			//	return;
			//}

			//ctl::ComPtr<DirectUI::DependencyObject> rootScrollViewerAsDO;
			//ctl::ComPtr<xaml_controls::IScrollViewer> rootScrollViewer;
			//IFCFAILFAST(DXamlCore::GetCurrent()->GetPeer(scrollContentControl, &rootScrollViewerAsDO));
			//IFCFAILFAST(rootScrollViewerAsDO.As(&rootScrollViewer));

			//if (rootScrollViewer)
			//{
			//	IFCFAILFAST(rootScrollViewer.Cast<RootScrollViewer>()->NotifyInputPaneStateChange(
			//		m_simulatedInputPaneOccludedRect.Height > 0 ? InputPaneState.InputPaneShowing : InputPaneState.InputPaneHidden,



			//{ m_simulatedInputPaneOccludedRect.X, m_simulatedInputPaneOccludedRect.Y, m_simulatedInputPaneOccludedRect.Width, m_simulatedInputPaneOccludedRect.Height}
			//     ));
			//}
		}
	}
}
