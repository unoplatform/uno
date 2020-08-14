using Android.App;
using Android.Content;

namespace Uno.UI
{
	/// <summary>
	/// OnActivityResultArgs : an object containing the parameters of the Activity.OnActivityResult
	/// </summary>
	/// <param name="ct">CancellationToken</param>
	/// <param name="intent">The Intent you want to send</param>
	/// <param name="requestCode">A specific Response code, this is useful if you send more than one type of request in parallel</param>
	public class OnActivityResultArgs
	{
		public OnActivityResultArgs(int requestCode, Result resultCode, Intent intent)
		{
			RequestCode = requestCode;
			ResultCode = resultCode;
			Intent = intent;
		}

		public Intent Intent { get; }
		public int RequestCode { get; }
		public Result ResultCode { get; }

	}
}
