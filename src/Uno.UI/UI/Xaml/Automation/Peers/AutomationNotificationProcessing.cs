namespace Windows.UI.Xaml.Automation.Peers;

/// <summary>
/// Specifies the order in which to process a notification.
/// </summary>
public enum AutomationNotificationProcessing
{
	/// <summary>
	/// These notifications should be presented to the user as soon as possible. All of the notifications from this source should be delivered to the user.
	/// Use this in a limited capacity as this style of message could cause a flooding for information to the end user due to the nature of the request to deliver all of the notifications.
	/// </summary>
	ImportantAll = 0,

	/// <summary>
	/// These notifications should be presented to the user as soon as possible. The most recent notifications from this source should be delivered to the user because the most recent notification supersedes all of the other notifications.
	/// </summary>
	ImportantMostRecent = 1,

	/// <summary>
	/// These notifications should be presented to the user when possible. All of the notifications from this source should be delivered to the user.
	/// </summary>
	All = 2,

	/// <summary>
	/// These notifications should be presented to the user when possible. Interrupt the current notification for this one.
	/// </summary>
	MostRecent = 3,

	/// <summary>
	/// These notifications should be presented to the user when possible. Don�t interrupt the current notification for this one. If new notifications come in from the same source while the current notification is being presented, then keep the most recent and ignore the rest until the current processing is completed. Then use the most recent message as the current message.
	/// </summary>
	CurrentThenMostRecent = 4,

}

