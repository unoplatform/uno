using System;
using System.Linq;

namespace Uno.UI.RemoteControl.HotReload.Messages;

public enum FileUpdateResult
{
	Success = 200,
	NoChanges = 204,
	// 300+ : errors cases (globally validated by casting to int)
	BadRequest = 400,
	FileNotFound = 404,
	Failed = 500,
	FailedToRequestHotReload = 502,
	NotAvailable = 503
}
