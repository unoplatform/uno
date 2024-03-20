using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;

namespace Windows.Storage.Pickers
{
	public static class FolderPickerHelper
	{
		public static void RegisterOnBeforeStartActivity(FolderPicker picker, Action<Intent> intentAction)
			=> picker.RegisterOnBeforeStartActivity(intentAction);
	}
}
