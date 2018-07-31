#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Core
{
	public  partial class CoreApplication 
	{
		public static event global::System.EventHandler<object> Resuming;

		public static event global::System.EventHandler<global::Windows.ApplicationModel.SuspendingEventArgs> Suspending;

		/// <summary>
		/// Raises the <see cref="Resuming"/> event
		/// </summary>
		internal static void RaiseResuming()
			=> Resuming?.Invoke(null, null);

		/// <summary>
		/// Raises the <see cref="Suspending"/> event
		/// </summary>
		internal static void RaiseSuspending(SuspendingEventArgs args)
			=> Suspending?.Invoke(null, args);
	}
}
