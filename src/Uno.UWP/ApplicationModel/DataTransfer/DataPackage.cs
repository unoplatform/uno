using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataPackage
	{
		internal string Text { get; private set; }
		public DataPackageOperation RequestedOperation { get; set; }

#if !(__ANDROID__ || NET461)
		public void SetText(string text)
		{
			if (value == null)
			{
				throw new ArgumentNullException("Text can't be null");
			}

			this.Text = value;
		}
#endif
	}
}
