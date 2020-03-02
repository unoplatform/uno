#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataPackageView
	{
		internal List<string> _availableFormats = new List<string>();
		internal string _text = null;
		internal string _html = null;
		internal Uri _uri = null;

		public IReadOnlyList<string> AvailableFormats => _availableFormats.AsReadOnly();

		public Foundation.IAsyncOperation<string> GetTextAsync()
		{
			if (_text is null)
			{
				throw new InvalidOperationException("DataPackage does not contain Text data.");
			}
			return Task.FromResult(_text).AsAsyncOperation();
		}
		public Foundation.IAsyncOperation<global::System.Uri> GetUriAsync()
		{
			if (_uri is null)
			{
				throw new InvalidOperationException("DataPackage does not contain Uri data.");
			}
			return Task.FromResult(_uri).AsAsyncOperation();
		}
		public Foundation.IAsyncOperation<string> GetHtmlFormatAsync()
		{
			if (_html is null)
			{
				throw new InvalidOperationException("DataPackage does not contain Html data.");
			}
			return Task.FromResult(_html).AsAsyncOperation();
		}
		public Foundation.IAsyncOperation<global::System.Uri> GetWebLinkAsync() => GetUriAsync();

		public bool Contains(string formatId)
		{
			if(formatId == StandardDataFormats.Text)
			{
				return (_text != null);
			}
			if (formatId == StandardDataFormats.Html)
			{
				return (_html != null);
			}
			if (formatId == StandardDataFormats.Uri || formatId == StandardDataFormats.WebLink)
			{
				return (_uri != null);
			}
			return false;
		}


	}
}
#endif
