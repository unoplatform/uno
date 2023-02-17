using System;

namespace Windows.UI.Xaml.Automation.Peers;

[Obsolete(
	"The Windows.UI.Xaml.Controls version of this control is not supported. " +
	"Please use Microsoft.UI.Xaml.Controls.PersonPicture instead.")]
public partial class PersonPictureAutomationPeer
{
	public PersonPictureAutomationPeer(PersonPicture owner) : base(owner)
	{
		throw new NotImplementedException(
			"The Windows.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft.UI.Xaml.Controls.PersonPicture instead.");
	}
}
