#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataPackage
	{
		internal string _html;
		internal Uri _uri;
		public void SetText(string text)
		{
			if (text == null)
			{
				throw new ArgumentNullException("Cannot set DataPackage.Text to null");
			}

			this.Text = text;
		}

		// Uri with namespace, as we have two different Uri (UWP and Android) here
		public void SetUri(global::System.Uri value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("Cannot set DataPackage.Uri to null");
			}

			this._uri = value;
		}

		public void SetHtmlFormat(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("Cannot set DataPackage.Html to null");
			}

			this._html = value;
		}
		// Uri with namespace, as we have two different Uri (UWP and Android) here
		public void SetWebLink(global::System.Uri value) => SetUri(value);

		public DataPackageView GetView()
		{
			// please, while changing it - synchronize it with Clipboard.GetContent()
			var clipView = new DataPackageView();

			if (this.Text != null)
			{
				clipView._availableFormats.Add(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text);
				clipView._text = this.Text;
			}

			if (this._html != null)
			{
				clipView._availableFormats.Add(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Html);
				clipView._html = this._html;
			}

			if (this._uri != null)
			{ // now, both Uri and WebLink == "UniformResourceLocatorW", but it can change
				clipView._availableFormats.Add(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Uri);
				clipView._availableFormats.Add(Windows.ApplicationModel.DataTransfer.StandardDataFormats.WebLink);
				clipView._uri = this._uri;
			}

			return clipView;

		}

	}
}

#endif
