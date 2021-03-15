using System;
using Foundation;
using Uno.Disposables;

namespace Uno.Storage.Internal
{
	internal static class NSUrlExtensions
    {
		public static IDisposable BeginSecurityScopedAccess(this NSUrl nsUrl)
		{
			try
			{
				var didStartAccessing = nsUrl.StartAccessingSecurityScopedResource();

				return Disposable.Create(() =>
				{
					if (didStartAccessing)
					{
						nsUrl.StopAccessingSecurityScopedResource();
					}
				});
			}
			catch (Exception ex)
			{
				throw new UnauthorizedAccessException("Could not access file system item. " + ex);
			}
		}
    }
}
