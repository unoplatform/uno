using Android.Util;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI.Controls
{
	public partial class BindableProgressBar : ProgressBar, DependencyObject
	{
		public BindableProgressBar()
			: base(Uno.UI.ContextHelper.Current)
		{
		}

		public BindableProgressBar(Android.Content.Context context)
			: base(context)
		{
			Initialize(context, null);
		}

		public BindableProgressBar(Android.Content.Context context, IAttributeSet attrs)
			: base(context, attrs)
		{
			Initialize(context, attrs);
		}

		public BindableProgressBar(Android.Content.Context context, IAttributeSet attrs, int defstyleAttr)
			: base(context, attrs, defStyleAttr: defstyleAttr)
		{
			Initialize(context, attrs);
		}

		public BindableProgressBar(Android.Content.Context context, IAttributeSet attrs, int defstyleAttr, int defStyleRes)
			: base(context, attrs, defstyleAttr, defStyleRes)
		{
			Initialize(context, attrs);
		}

		private void Initialize(Android.Content.Context context, IAttributeSet attrs)
		{
			InitializeBinder();
		}
	}
}
