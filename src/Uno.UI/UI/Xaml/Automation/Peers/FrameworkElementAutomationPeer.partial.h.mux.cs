using Uno.UI.DataBinding;

namespace Windows.UI.Xaml.Automation.Peers;

partial class FrameworkElementAutomationPeer
{
	private ManagedWeakReference? m_wpOwner;
	private string m_LocalizedControlType;
	private string m_ClassName;
	private ScrollItemAdapter m_spScrollItemAdapter;

	private AutomationControlType m_ControlType;

}
