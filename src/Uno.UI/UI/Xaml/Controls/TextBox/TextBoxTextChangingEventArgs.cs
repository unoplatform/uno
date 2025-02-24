using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public sealed partial class TextBoxTextChangingEventArgs
	{
		internal TextBoxTextChangingEventArgs()
		{
			// From UWP docs: "This event is fired for a format or content change. The IsContentChanging property helps to distinguish when text content is changing."
			// What does 'format change' mean for TextBox? At the moment Uno only raises event when text content changes.
			IsContentChanging = true;
		}
		public bool IsContentChanging { get; }
	}
}
