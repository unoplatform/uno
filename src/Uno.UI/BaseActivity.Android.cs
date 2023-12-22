using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml;
using Android.OS;
using Windows.UI.ViewManagement;
using Uno.UI.Xaml.Controls;
using Windows.UI.WindowManagement;
using AppWindow = Microsoft.UI.Windowing.AppWindow;

namespace Uno.UI
{
	[Activity(

		// This is required for OnConfigurationChanges to be raised.
		ConfigurationChanges =
			  Android.Content.PM.ConfigChanges.Orientation
			| Android.Content.PM.ConfigChanges.ScreenSize
	)]
#pragma warning disable 618
	public partial class BaseActivity : AndroidX.AppCompat.App.AppCompatActivity, DependencyObject
#pragma warning restore 618

	{
		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{D3D9799C-E65D-4A71-AE1E-1A3CB77B2492}");

			public const int TouchStart = 1;
			public const int TouchStop = 2;
		}

		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		public const string CreatedTotalBindableActivityCounter = "BindableActivity.CreatedTotal";
		public const string ActiveBindableActivityCounter = "BindableActivity.ActiveCount";
		public const string DisposedTotalBindableActivityCounter = "BindableActivity.DisposedCount";

		/// <summary>
		/// Occurs when an instance of BaseActivity is created or destroyed
		/// </summary>
		public static event EventHandler<ActivitiesCollectionChangedEventArgs> InstancesChanged;

		/// <summary>
		/// Occurs when the <see cref="Current"/> activity changed.
		/// </summary>
		public static event EventHandler<CurrentActivityChangedEventArgs> CurrentChanged;

		private static int _instanceCount;
		private static Dictionary<int, BaseActivity> _instances = new Dictionary<int, BaseActivity>();
		private static BaseActivity _current;

		/// <summary>
		/// Unique identifier for this instance of an activity.
		/// </summary>
		public int Id { get; } = Interlocked.Increment(ref _instanceCount);

		/// <summary>
		/// Gets a list of all activities which are currently alive.
		/// </summary>
		public static IImmutableDictionary<int, BaseActivity> Instances
			=> ImmutableDictionary<int, BaseActivity>.Empty.AddRange(_instances);

		/// <summary>
		/// Gets the currently running activity, if any.
		/// <remarks>
		/// This is the BaseActivity which is currently running, which means that it was "Resumed" and not "Paused". 
		/// This may be null if the current activity does not inherit from BaseActivity, or is not yet "Created".
		/// For more info look at the lifecycle diagram documented here: https://developer.android.com/reference/android/app/Activity.html.
		/// </remarks>
		/// </summary>
		public static BaseActivity Current => _current;

		/// <summary>
		/// Gets the first BaseActivity which is set as current. Unlike the <see cref="Current"/>, this method will wait until a BaseActivity is set as Current.
		/// </summary>
		/// <param name="ct"></param>
		/// <returns>The first BaseActivity displayed</returns>
		public static async Task<BaseActivity> GetCurrent(CancellationToken ct)
		{
			// Fast path
			var current = Current;
			if (current != null)
			{
				return current;
			}

			var asyncCurrent = new TaskCompletionSource<BaseActivity>();
			var handler = new EventHandler<CurrentActivityChangedEventArgs>((cnd, args) =>
			{
				if (args.Current != null)
				{
					asyncCurrent.TrySetResult(args.Current);
				}
			});

			try
			{
				CurrentChanged += handler;

				// Check if updated since initial check
				current = Current;
				if (current != null)
				{
					return current;
				}

				using (ct.Register(() => asyncCurrent.TrySetCanceled()))
				{
					return await asyncCurrent.Task;
				}
			}
			finally
			{
				CurrentChanged -= handler;
			}
		}

		public BaseActivity(IntPtr handle, JniHandleOwnership transfer)
			: base(handle, transfer)
		{
			InitializeBinder();
			ContextHelper.Current = this;
			Initialize();

#if !IS_UNO
			Performance.Increment(CreatedTotalBindableActivityCounter);
			Performance.Increment(ActiveBindableActivityCounter);
#endif
		}

		public BaseActivity()
		{
			InitializeBinder();
			ContextHelper.Current = this;
			Initialize();

#if !IS_UNO
			Performance.Increment(CreatedTotalBindableActivityCounter);
			Performance.Increment(ActiveBindableActivityCounter);
#endif
		}

		private void Initialize()
		{
			// Eagerly create the ApplicationView instance for IBaseActivityEvents
			// to be useable (specifically for the Create event)
			ApplicationView.InitializeForWindowId(AppWindow.MainWindowId);

			NotifyCreatingInstance();
		}

		partial void InnerAttachedToWindow() => BinderAttachedToWindow();

		partial void InnerDetachedFromWindow() => BinderDetachedFromWindow();

		public View ContentView { get; private set; }

		public override void SetContentView(View view)
		{
			ContentView = view;
			base.SetContentView(view);
		}

		public override void SetContentView(View view, ViewGroup.LayoutParams @params)
		{
			ContentView = view;
			base.SetContentView(view, @params);
		}

		public override void AddContentView(View view, ViewGroup.LayoutParams @params)
		{
			ContentView = view;
			base.AddContentView(view, @params);
		}

		#region Activity LifeCycle cf. https://developer.android.com/reference/android/app/Activity.html

		partial void InnerCreate(Android.OS.Bundle savedInstanceState) => SetAsCurrent();

		partial void InnerCreateWithPersistedState(Bundle savedInstanceState, PersistableBundle persistentState) => SetAsCurrent();

		partial void InnerStart()
		{
			SetAsCurrent();

			Microsoft.UI.Xaml.Application.Current?.RaiseLeavingBackground(() =>
			{
				NativeWindowWrapper.Instance.OnNativeVisibilityChanged(true);
			});
		}

		partial void InnerRestart() => SetAsCurrent();

		partial void InnerResume()
		{
			SetAsCurrent();

			Microsoft.UI.Xaml.Application.Current?.RaiseResuming();
			NativeWindowWrapper.Instance.OnNativeActivated(CoreWindowActivationState.CodeActivated);
		}

		partial void InnerTopResumedActivityChanged(bool isTopResumedActivity)
		{
			NativeWindowWrapper.Instance.OnNativeActivated(
				isTopResumedActivity ?
					CoreWindowActivationState.CodeActivated :
					CoreWindowActivationState.Deactivated);
		}

		partial void InnerPause()
		{
			ResignCurrent();

			NativeWindowWrapper.Instance.OnNativeActivated(CoreWindowActivationState.Deactivated);
		}

		partial void InnerStop()
		{
			ResignCurrent();

			NativeWindowWrapper.Instance.OnNativeVisibilityChanged(false);
			Microsoft.UI.Xaml.Application.Current?.RaiseEnteredBackground(() => Microsoft.UI.Xaml.Application.Current?.RaiseSuspending());
		}

		partial void InnerDestroy() => ResignCurrent();

		private void SetAsCurrent()
		{
			ContextHelper.Current = this;
			if (Interlocked.Exchange(ref _current, this) != this)
			{
				_current = this;
				CurrentChanged?.Invoke(this, new CurrentActivityChangedEventArgs(this));
			}
		}

		private void ResignCurrent()
		{
			if (Interlocked.CompareExchange(ref _current, null, this) == this)
			{
				CurrentChanged?.Invoke(this, new CurrentActivityChangedEventArgs(null));
			}
		}
		#endregion

		#region Instance discovery management

		private void NotifyCreatingInstance()
		{
			lock (_instances)
			{
				_instances.Add(Id, this);
			}

			InstancesChanged?.Invoke(null, ActivitiesCollectionChangedEventArgs.Added(Id, Instances));
		}

		private void NotifyDestroyingInstance(bool isFinalizer)
		{
			try
			{
				lock (_instances)
				{
					_instances.Remove(Id);
				}

				DispatchedHandler notify = () =>
				{
					try
					{
						InstancesChanged?.Invoke(null, ActivitiesCollectionChangedEventArgs.Removed(Id, Instances));
					}
					catch (Exception e)
					{
						this.Log().Error("An exception was thrown while notifying instance collection changed.", e);
					}
				};

				if (isFinalizer)
				{
					_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, notify);
				}
				else
				{
					notify();
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to remove activity from instances collection.", e);
			}
		}
		#endregion

		protected sealed override void Dispose(bool disposing)
		{
			try
			{
				base.Dispose(disposing);

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("Disposing {0}", disposing);
				}

				NotifyDestroyingInstance(isFinalizer: !disposing);

				if (disposing)
				{
#if !IS_UNO
					Performance.Decrement(ActiveBindableActivityCounter);
#endif
				}
			}
			catch (Exception e)
			{
				this.Log().ErrorFormat("Failed to dispose view", e);
			}
		}

		public override bool DispatchTouchEvent(MotionEvent ev)
		{
			if (_trace.IsEnabled)
			{
				if (ev.Action == MotionEventActions.Down)
				{
					_trace.WriteEvent(TraceProvider.TouchStart);
				}
				if (ev.Action == MotionEventActions.Up)
				{
					_trace.WriteEvent(TraceProvider.TouchStop);
				}
			}

			return base.DispatchTouchEvent(ev);
		}

		public virtual IEnumerable<IDataContextProvider> GetChildrenProviders() =>
			new[] { ContentView as IDataContextProvider }
			.Trim();

		~BaseActivity()
		{
			this.Log().Error("~BaseActivity()");

#if !IS_UNO
			Performance.Decrement(ActiveBindableActivityCounter);
#endif
		}
	}
}
