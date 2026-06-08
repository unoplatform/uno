#if __ANDROID__
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;

namespace UITests.Shared.Windows_UI_Xaml.UIElementTests
{
	/// <summary>
	/// A native ScrollView with a long text inside
	/// </summary>
	public partial class NativeView : ScrollView
	{
		public NativeView() : base(ContextHelper.Current)
		{
			var txt = new TextView(ContextHelper.Current)
			{
				Text = GetLongText(),
				Background = new ColorDrawable(Color.Yellow)
			};

			this.AddChild(txt);
		}

		private string GetLongText()
		{
			var rnd = new Random(5847900);
			var sb = new StringBuilder();

			for (int i = 0; i < 5000; i++)
			{
				var val = rnd.Next(0, 10);

				sb.Append(val == 0 ? " " : val.ToString());
			}

			return sb.ToString();
		}
	}
}
#endif
