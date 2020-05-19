using Android.App;
using Android.Views;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using System;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Fragment = AndroidX.Fragment.App.Fragment;

namespace Uno.UI
{
	public abstract partial class BaseFragment : Fragment, DependencyObject, View.IOnTouchListener
	{
		private bool _isDisposed = false;

		private readonly Func<IFragmentTracker> _tracker;

		public static Func<IFragmentTracker> DefaultFragmentTracker;

		public BaseFragment()
		{
			_tracker = DefaultFragmentTracker;
			_tracker = _tracker.AsMemoized();

			InitializeBinder();
		}

#pragma warning disable 0672,618
		public override void OnAttach(Activity activity)
		{
			base.OnAttach(activity);

			BinderAttachedToWindow();
		}
#pragma warning restore 0672,618

		public override void OnDetach()
		{
			base.OnDetach();

			BinderDetachedFromWindow();
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Android.OS.Bundle savedInstanceState)
		{
			InitializeComponent();

			//We want every fragment to intercept the touch to ensure a previous fragment cannot be touched through the current one
			Content?.SetOnTouchListener(this);

			return Content;
		}

		protected abstract void InitializeComponent();

		public View Content { get; protected set; }
		
		public override void OnCreate(Android.OS.Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			_tracker().PushCreated(this);
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			_tracker().PushDestroyed(this);
		}
		
		public override void OnResume()
		{
			base.OnResume();
			_tracker().PushResume(this);
		}
		
		public override void OnViewCreated(View view, Android.OS.Bundle savedInstanceState)
		{
			base.OnViewCreated(view, savedInstanceState);
			_tracker().PushViewCreated(this);
		}

		public override void OnPause()
		{
			base.OnPause();
			_tracker().PushPause(this);
		}
		
		public override void OnStart()
		{
			base.OnStart();
			_tracker().PushStart(this);
		}

		public override void OnStop()
		{
			base.OnStop();
			_tracker().PushStop(this);
		}

		public virtual bool OnBackPressed()
		{
			return true;
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				base.Dispose(disposing);

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("Disposing {0}", disposing);
				}

				if (!_isDisposed)
				{
					Content.SafeDispose();
				}
			}
			catch (Exception e)
			{
				this.Log().ErrorFormat("Failed to dispose view", e);
			}
		}

		public virtual IEnumerable<IDataContextProvider> GetChildrenProviders() => 
			new[] { Content as IDataContextProvider }
			.Trim();
		
		public bool OnTouch(View v, MotionEvent e)
		{
			return true;
		}
	}
}
