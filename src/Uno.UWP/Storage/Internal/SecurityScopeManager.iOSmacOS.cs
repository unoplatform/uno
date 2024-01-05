using System;
using System.Collections.Generic;
using Foundation;
using Uno.Disposables;

namespace Windows.Storage.Internal
{
	public static class SecurityScopeManager
	{
		private static readonly object _syncLock = new object();
		private static readonly Dictionary<string, RefCountDisposable> _activeScopes = new Dictionary<string, RefCountDisposable>();

		public static IDisposable BeginScope(NSUrl url)
		{
			lock (_syncLock)
			{
				var absoluteString = url.AbsoluteString!;
				if (!_activeScopes.TryGetValue(absoluteString, out var disposable) || disposable.IsDisposed)
				{
					bool scopeStarted = false;
					try
					{
						scopeStarted = url.StartAccessingSecurityScopedResource();
					}
					catch (Exception ex)
					{
						throw new UnauthorizedAccessException("Could not access file system item. " + ex);
					}
					if (scopeStarted)
					{
						disposable = new RefCountDisposable(Disposable.Create(() =>
						{
							lock (_syncLock)
							{
								url.StopAccessingSecurityScopedResource();
								_activeScopes.Remove(absoluteString);
							}
						}));
						_activeScopes[absoluteString] = disposable;
						return disposable;
					}
					else
					{
						return Disposable.Empty;
					}
				}
				else
				{
					return disposable.GetDisposable();
				}
			}
		}
	}
}
