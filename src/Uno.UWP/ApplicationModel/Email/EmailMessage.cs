using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace Windows.ApplicationModel.Email
{
	public partial class EmailMessage
	{
		private string _subject = string.Empty;
		private string _body = string.Empty;

		public EmailMessage()
		{
		}

		public string Subject
		{
			get => _subject;
			set
			{
				_subject = value ?? throw new ArgumentNullException(nameof(value));
			}
		}

		public string Body
		{
			get => _body;
			set
			{
				_body = value ?? throw new ArgumentNullException(nameof(value));
			}
		}

		public IList<EmailRecipient> Bcc { get; } = new NonNullList<EmailRecipient>();

		public IList<EmailRecipient> CC { get; } = new NonNullList<EmailRecipient>();

		public IList<EmailRecipient> To { get; } = new NonNullList<EmailRecipient>();

		public EmailRecipient Sender { get; set; } = new EmailRecipient();

		internal Uri ToMailtoUri()
		{
			var uriBuilder = new StringBuilder();
			uriBuilder.Append("mailto:?");

			if (To.Count > 0)
			{
				uriBuilder.Append("to=" + Uri.EscapeDataString(string.Join(",", To.Select(m => m.Address))));
				uriBuilder.Append('&');
			}
			if (CC.Count > 0)
			{
				uriBuilder.Append("cc=" + Uri.EscapeDataString(string.Join(",", CC.Select(m => m.Address))));
				uriBuilder.Append('&');
			}
			if (Bcc.Count > 0)
			{
				uriBuilder.Append("bcc=" + Uri.EscapeDataString(string.Join(",", Bcc.Select(m => m.Address))));
				uriBuilder.Append('&');
			}
			if (!string.IsNullOrEmpty(Body))
			{
				uriBuilder.Append("body=" + Uri.EscapeDataString(Body));
				uriBuilder.Append('&');
			}
			if (!string.IsNullOrEmpty(Subject))
			{
				uriBuilder.Append("subject=" + Uri.EscapeDataString(Subject));
				uriBuilder.Append('&');
			}

			if (uriBuilder[uriBuilder.Length - 1] == '&')
			{
				//trim
				uriBuilder.Remove(uriBuilder.Length - 1, 1);
			}

			return new Uri(uriBuilder.ToString(), UriKind.Absolute);
		}
	}
}
