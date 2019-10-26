using System.Collections.Generic;

namespace Windows.ApplicationModel.Email
{
	public partial class EmailMessage
	{
		public string Subject { get; set; }

		public string Body { get; set; }

		public IList<EmailRecipient> Bcc { get; } = new List<EmailRecipient>();

		public IList<EmailRecipient> CC { get; } = new List<EmailRecipient>();

		public IList<EmailAttachment> Attachments { get; } = new List<EmailAttachment>();

		public IList<EmailRecipient> To { get; } = new List<EmailRecipient>();
		
		public IList<EmailRecipient> ReplyTo { get; } = new List<EmailRecipient>();
	}
}
