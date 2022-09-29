#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.ApplicationModel.Chat
{
	public enum ChatStoreChangedEventKind
	{
		NotificationsMissed,
		StoreModified,
		MessageCreated,
		MessageModified,
		MessageDeleted,
		ConversationModified,
		ConversationDeleted,
		ConversationTransportDeleted,
	}
}
