﻿using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Uno.UI;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public abstract class NativePage : BaseActivity
	{
		public NativePage(IntPtr ptr, JniHandleOwnership owner) : base(ptr, owner)
		{
		}

		public NativePage()
		{
		}

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			InitializeComponent();

			var decorView = (ContextHelper.Current as Activity).Window.DecorView;

#pragma warning disable 618
#pragma warning disable CA1422 // Validate platform compatibility
			NativeWindowWrapper.Instance.SystemUiVisibility = (int)decorView.SystemUiVisibility;
			decorView.SetOnSystemUiVisibilityChangeListener(new OnSystemUiVisibilityChangeListener());
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			// By detaching the ContentView after destroying the page,
			// the same ContentView can be reused by other activities.
			(ContentView?.Parent as ViewGroup)?.RemoveView(ContentView);
		}

		protected abstract void InitializeComponent();

		public View Content
		{
			get
			{
				return ContentView;
			}
			set
			{
				SetContentView(value, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
			}
		}
	}
}
