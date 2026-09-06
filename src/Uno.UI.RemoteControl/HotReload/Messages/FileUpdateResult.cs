#if UNO_HOTRELOAD // HR Engine + Dev Server (JSON contract with client)
namespace Uno.HotReload.IO;
#elif UNO_RC_MESSAGING // IDE <-> Dev-Server
namespace Uno.UI.RemoteControl.Messaging.IdeChannel.HotReload;
#else // Client
namespace Uno.UI.RemoteControl.HotReload.Messages;
#endif

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
