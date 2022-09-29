#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.ApplicationModel.Chat
{
	public enum ChatMessageChangeType
	{
		MessageCreated,
		MessageModified,
		MessageDeleted,
		ChangeTrackingLost,
	}
}
