#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.ApplicationModel.Chat
{
	public enum ChatMessageStatus
	{
		Draft,
		Sending,
		Sent,
		SendRetryNeeded,
		SendFailed,
		Received,
		ReceiveDownloadNeeded,
		ReceiveDownloadFailed,
		ReceiveDownloading,
		Deleted,
		Declined,
		Cancelled,
		Recalled,
		ReceiveRetryNeeded,
	}
}
