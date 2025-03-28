using System;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Input
{
	internal class UnsafeFocusLockOverrideGuard : IDisposable
	{
		private readonly FocusManager _focusManager;

		public UnsafeFocusLockOverrideGuard(FocusManager focusManager)
		{
			_focusManager = focusManager ?? throw new ArgumentNullException(nameof(focusManager));
			_focusManager.SetIgnoreFocusLock(true);
		}

		public void Dispose() => _focusManager.SetIgnoreFocusLock(false);
	}
}
