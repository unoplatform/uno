#if !NET461
using System;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataPackage
	{
		internal string Text { get; private set; }

		internal string Html { get; private set; }

		internal Uri Uri { get; private set; }

		public DataPackageOperation RequestedOperation { get; set; }

		public void SetText(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("Text can't be null");
			}

			Text = value;
		}

		public void SetUri(Uri value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("Cannot set DataPackage.Uri to null");
			}

			Uri = value;
		}

		public void SetHtmlFormat(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("Cannot set DataPackage.Html to null");
			}

			Html = value;
		}

		public void SetWebLink(Uri value) => SetUri(value);

		public DataPackageView GetView()
		{
			var clipView = new DataPackageView();

			if (Text != null)
			{
				clipView.SetFormatTask(StandardDataFormats.Text, Task.FromResult(Text));
			}

			if (Html != null)
			{
				clipView.SetFormatTask(StandardDataFormats.Html, Task.FromResult(Html));
			}

			if (Uri != null)
			{
				clipView.SetFormatTask(StandardDataFormats.Uri, Task.FromResult(Uri));
				clipView.SetFormatTask(StandardDataFormats.WebLink, Task.FromResult(Uri));
			}

			return clipView;
		}
	}
}
#endif
