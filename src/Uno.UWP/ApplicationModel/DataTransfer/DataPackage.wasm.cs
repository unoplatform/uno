using System;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataPackage
	{
		public void SetUri(Uri value)
		{
			// URI is treat as text in WASM
			Text = value.ToString();
		}
	}
}