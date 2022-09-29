#nullable disable

#if __ANDROID__
using Android.Content;
using Android.Views.InputMethods;
using Java.Interop;
using Uno.UI;

namespace Windows.Globalization
{
	public partial class Language
	{
		public static string CurrentInputMethodLanguageTag
		{
			get
			{
				var inputMethodManager = ContextHelper.Current?.GetSystemService(Context.InputMethodService).JavaCast<InputMethodManager>();
				var currentInputMethod = inputMethodManager?.CurrentInputMethodSubtype;
				return currentInputMethod?.LanguageTag ?? "";
			}
		}

		public static bool TrySetInputMethodLanguageTag(string languageTag) => false;
	}
}
#endif
