using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation.Collections;

namespace Windows.ApplicationModel.Chat
{
	public partial class ChatMessage : IChatItem
	{
		private string _body = "";

		public ChatMessage()
		{
		}

		public string Body
		{
			get => _body;
			set
			{
				//UWP does not allow null body
				_body = value ?? throw new ArgumentNullException(nameof(value));
			}
		}

		public ChatItemKind ItemKind { get; }
		public bool IsIncoming { get; set; }
		public ChatMessageStatus Status { get; set; }
		public string? From { get; set; } = "";
		public DateTimeOffset LocalTimestamp { get; set; }
		public ChatMessageKind MessageKind { get; set; }
		public ChatMessageOperatorKind MessageOperatorKind { get; set; }
		public bool IsRead { get; set; }
		public DateTimeOffset NetworkTimestamp { get; set; }
		public bool IsSeen { get; set; }

		public IList<string> Recipients { get; } = new NonNullList<string>();
	}
}
