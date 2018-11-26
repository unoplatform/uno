using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataPackage
	{
		internal string Text { get; private set; }

		public void SetText(string text)
		{
			this.Text = text;
		}
	}
}
