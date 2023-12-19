#nullable enable
using System;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Tests.Enterprise
{
	internal class SafeEventRegistration<TElement, TDelegate>
		where TElement : class
		where TDelegate : Delegate
	{
		private readonly EventInfo _eventInfo;
		private IDisposable? _last;

		public SafeEventRegistration(string eventName)
		{
			_eventInfo = typeof(TElement).GetEvent(eventName)!;
		}

		internal IDisposable Attach(TElement element, TDelegate handler)
		{
			Detach(); // Detach any previous handler

			object? token = _eventInfo.GetAddMethod()!.Invoke(element, new object[] { handler });

			return _last = Disposable.Create(() => 
			{
				// On Windows, token is EventRegistrationToken, on Uno, token is null
#if WINAPPSDK
				RunOnUIThread.Execute(() => _eventInfo.GetRemoveMethod()!.Invoke(element, new object?[] { token }));
#else
				RunOnUIThread.Execute(() => _eventInfo.GetRemoveMethod()!.Invoke(element, new object?[] { handler }));
#endif
			});
		}

		internal void Detach() => _last?.Dispose(); 
	}
}
