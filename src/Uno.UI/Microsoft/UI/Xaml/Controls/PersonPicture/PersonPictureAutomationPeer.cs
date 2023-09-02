using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class PersonPictureAutomationPeer : FrameworkElementAutomationPeer
{
	public PersonPictureAutomationPeer(PersonPicture owner) : base(owner)
	{
	}

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

	protected override string GetClassNameCore() => nameof(PersonPicture);
}
