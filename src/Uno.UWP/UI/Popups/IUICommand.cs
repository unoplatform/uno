using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Popups
{
	public partial interface IUICommand
	{
		/// <summary>
		/// Gets or sets the identifier of the command.
		/// </summary>
		object Id { get; set; }
		
		/// <summary>
		/// Gets or sets the handler for the event that is fired when the user invokes the command. 
		/// </summary>
		UICommandInvokedHandler Invoked { get; set; }

		/// <summary>
		/// Gets or sets the label for the command.
		/// </summary>
		string Label {get; set;}
    }
}
