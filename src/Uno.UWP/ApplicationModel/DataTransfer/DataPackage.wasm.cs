using System;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataPackage
	{
		internal string Text;

		public void SetText(string value)
		{
			Text = value;
		}

		public void SetUri(Uri value)
		{
			// URI is treat as text in WASM
			Text = value.ToString();
		}
	}
}