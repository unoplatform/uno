using System;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers;

[Obsolete(
	"The Windows.UI.Xaml.Controls version of this control is not supported. " +
	"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.PersonPicture instead.")]
public partial class PersonPictureAutomationPeer
{
	public PersonPictureAutomationPeer(PersonPicture owner) : base(owner)
	{
		throw new NotImplementedException(
			"The Windows.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.PersonPicture instead.");
	}
}
