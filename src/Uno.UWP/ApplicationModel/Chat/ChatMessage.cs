using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation.Collections;

namespace Windows.ApplicationModel.Chat
{
	public partial class ChatMessage : IChatItem
	{
		private string _body;

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

		public IList<string> Recipients { get; } = new NonNullList<string>();
	}
}
