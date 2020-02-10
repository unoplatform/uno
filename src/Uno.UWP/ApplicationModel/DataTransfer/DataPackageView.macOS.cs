#if __MACOS__
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataPackageView
	{
		private List<string> _availableFormats = new List<string>();
		private string _text = null;

		public IReadOnlyList<string> AvailableFormats => _availableFormats.AsReadOnly();

		internal void SetText(string text)
		{
			_text = text;
			if (_text != null)
			{
				_availableFormats.Add(StandardDataFormats.Text);
			}
			else
			{
				_availableFormats.Remove(StandardDataFormats.Text);
			}
		}

		public Foundation.IAsyncOperation<string> GetTextAsync()
		{
			if (!_availableFormats.Contains(StandardDataFormats.Text))
			{
				throw new InvalidOperationException("DataPackage does not contain Text data.");
			}
			return Task.FromResult(_text).AsAsyncOperation();
		}
	}
}
#endif
