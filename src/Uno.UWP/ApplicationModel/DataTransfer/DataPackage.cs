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
			=> Text = value ?? throw new ArgumentNullException("Text can't be null");

		public void SetUri(Uri value)
			=> Uri = value ?? throw new ArgumentNullException("Cannot set DataPackage.Uri to null");

		public void SetHtmlFormat(string value)
			=> Html = value ?? throw new ArgumentNullException("Cannot set DataPackage.Html to null");

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
