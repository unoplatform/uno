#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;

namespace Uno.Helpers.Activities
{
	internal class ActivityResult
	{
		public ActivityResult(int requestCode, Result result, Intent intent)
		{
			RequestCode = requestCode;
			Result = result;
			Intent = intent;
		}

		public int RequestCode { get; }

		public Result Result { get; }

		public Intent Intent { get; }
	}
}
#endif
