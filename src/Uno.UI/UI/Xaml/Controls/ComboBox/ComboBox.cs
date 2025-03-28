using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Represents a selection control that combines a non-editable text box and a drop-down list box that allows users to select an item from a list.
/// </summary>
[InputProperty(Name = "Text")]
public partial class ComboBox : Selector
{
	/// <summary>
	/// Invoked when the DropDownClosed event is raised.
	/// </summary>
	/// <param name="e">Event data for the event.</param>
	protected virtual void OnDropDownClosed(object e) => DropDownClosed?.Invoke(this, e);

	/// <summary>
	/// Invoked when the DropDownOpened event is raised.
	/// </summary>
	/// <param name="e">Event data for the event.</param>
	protected virtual void OnDropDownOpened(object e) => DropDownOpened?.Invoke(this, e);
}
