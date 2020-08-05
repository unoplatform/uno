/*

Implementation based on https://github.com/roubachof/Sharpnado.MaterialFrame.
with some modifications and removal of unused features.

*/

using System;

namespace Uno.UI.Xaml.Media
{
    internal class JniWeakReference<T>
        where T : Java.Lang.Object
    {
        private readonly WeakReference<T> _reference;

        public JniWeakReference(T target)
        {
            _reference = new WeakReference<T>(target);
        }

        public bool TryGetTarget(out T target)
        {
            target = null;
            if (_reference.TryGetTarget(out var innerTarget))
            {
                if (innerTarget.Handle != IntPtr.Zero)
                {
                    target = innerTarget;
                }
            }

            return target != null;
        }

		public override string ToString() => $"[JniWeakReference] {_reference}";
	}
}
