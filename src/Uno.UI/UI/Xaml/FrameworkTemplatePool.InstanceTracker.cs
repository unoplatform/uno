#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Windows.UI.Xaml
{
	public partial class FrameworkTemplatePool
	{
		/// <summary>
		/// The InstanceTracker allows children to be returned to the <see cref="FrameworkTemplatePool"/>.
		/// It does so by tying the lifetime of the parent to their children using <see cref="DependentHandle"/>
		/// and <see cref="TrackerCookie">TrackerCookie</see> without creating strong references.
		/// Once a parent is collected, the cookie becomes eligible for gc, its finalizer will run,
		/// the child will set its parent to null, making itself available to the pool.
		/// </summary>
		private static class InstanceTracker
		{
			private static readonly Stack<TrackerCookie> _cookiePool = new();

			private static readonly List<DependentHandle> _handles = new();
			private static readonly Stack<int> _handlesFreeList = new();

			private static int _counter;

			private const int HandleCleanupInterval = 1024;
			private const int MaxCookiePoolSize = 256;

			public static void Add(object parent, object instance)
			{
				TrackerCookie? cookie;

				lock (_cookiePool)
				{
					// Cookies are pooled because we create lots of them.
					if (_cookiePool.TryPop(out cookie))
					{
						cookie.Update(instance);
					}
					else
					{
						cookie = new TrackerCookie(instance);
					}
				}

				// Try to get a free slot in the list, this avoids scanning the list everytime
				if (_handlesFreeList.TryPop(out var index))
				{
					ref var handle = ref CollectionsMarshal.AsSpan(_handles)[index];

					handle = new DependentHandle(parent, cookie);
				}
				else
				{
					// No slots are available, try to scrub the list, this is necessary because
					// we don't want to leak handles (coreclr) or ephemerons (mono)
					if ((_counter++ % HandleCleanupInterval) == 0)
					{
						var handles = CollectionsMarshal.AsSpan(_handles);

						for (var x = 0; x < handles.Length; x++)
						{
							ref var handle = ref handles[x];

							if (handle.IsAllocated && handle.Target == null)
							{
								handle.Dispose();

								_handlesFreeList.Push(x);
							}
						}

						// Maybe a slot is available now
						if (_handlesFreeList.TryPop(out index))
						{
							ref var handle = ref handles[index];

							handle = new DependentHandle(parent, cookie);

							return;
						}
					}

					// No slots are available
					_handles.Add(new DependentHandle(parent, cookie));
				}
			}

			public static void TryReturnCookie(TrackerCookie cookie)
			{
				lock (_cookiePool)
				{
					// The pool isn't full, resurrect the cookie so its finalizer will run again
					if (_cookiePool.Count < MaxCookiePoolSize)
					{
						GC.ReRegisterForFinalize(cookie);

						_cookiePool.Push(cookie);
					}
				}
			}

			public class TrackerCookie
			{
				private object? _instance;

				public TrackerCookie(object instance)
				{
					_instance = instance;
				}

				~TrackerCookie()
				{
					Instance.RaiseOnParentCollected(_instance!);

					_instance = null;

					TryReturnCookie(this);
				}

				public void Update(object instance) => _instance = instance;
			}
		}
	}
}
