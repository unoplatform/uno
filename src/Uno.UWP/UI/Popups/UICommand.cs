using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Popups
{
	public sealed partial class UICommand : IUICommand
	{
		/// <summary>
		/// Creates a new instance of the UICommand class.
		/// </summary>
		public UICommand()
			: this("", null, null)
		{
		}

		/// <summary>
		/// Creates a new instance of the UICommand class using the specified label.
		/// </summary>
		/// <param name="label"></param>
		public UICommand(string label)
			: this(label, null, null)
		{
		}

		/// <summary>
		///  Creates a new instance of the UICommand class using the specified label and optional event handler.
		/// </summary>
		/// <param name="label"></param>
		/// <param name="action"></param>
		public UICommand(string label, UICommandInvokedHandler action)
			: this(label, action, null)
		{
		}

		/// <summary>
		/// Creates a new instance of the UICommand class using the specified label, and optional event handler and command identifier.
		/// </summary>
		/// <param name="label"></param>
		/// <param name="action"></param>
		/// <param name="commandId"></param>
		public UICommand(string label, UICommandInvokedHandler action, object commandId)
		{
			if (label == null)
			{
				throw new ArgumentNullException(nameof(label));
			}

			Label = label;
			// These can be null
			Invoked = action;
			Id = commandId;
		}

		/// <summary>
		/// Gets or sets the identifier of the command.
		/// </summary>
		public object Id { get; set; }

		/// <summary>
		/// Gets or sets the handler for the event that is fired when the user invokes the command. 
		/// </summary>
		public UICommandInvokedHandler Invoked { get; set; }

		/// <summary>
		/// Gets or sets the label for the command.
		/// </summary>
		public string Label { get; set; }
	}
}
