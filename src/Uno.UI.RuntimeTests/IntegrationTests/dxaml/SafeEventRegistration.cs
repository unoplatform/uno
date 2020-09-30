#nullable enable
using System;
using System.Reflection;
using System.Reflection.Emit;
using Windows.UI.Xaml.Controls;
using Uno.Disposables;

namespace Windows.UI.Xaml.Tests.Enterprise
{
	internal class SafeEventRegistration<TElement, TDelegate>
		where TElement : class
		where TDelegate : Delegate
	{
		private readonly EventInfo _eventInfo;
		private IDisposable? _last;

		public SafeEventRegistration(string eventName)
		{
			_eventInfo = typeof(TElement).GetEvent(eventName);
		}

		internal IDisposable Attach(TElement element, TDelegate handler)
		{
			Detach(); // Detach any previous handler

			_eventInfo.GetAddMethod().Invoke(element, new object[] {handler});

			return _last = Disposable.Create(() =>
			{
				_eventInfo.GetRemoveMethod().Invoke(element, new object[] {handler});
			});
		}

		internal void Detach() => _last?.Dispose();
	}
}
