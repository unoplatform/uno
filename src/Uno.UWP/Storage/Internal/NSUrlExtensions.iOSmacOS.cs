using System;
using Foundation;
using Uno.Disposables;

namespace Uno.Storage.Pickers.Internal
{
	internal static class NSUrlExtensions
    {
		public static IDisposable BeginSecurityScopedAccess(this NSUrl nsUrl)
		{
			if (!nsUrl.StartAccessingSecurityScopedResource())
			{
				throw new UnauthorizedAccessException("Could not access security-scoped resource.");
			}

			return Disposable.Create(() =>
			{
				nsUrl.StopAccessingSecurityScopedResource();
			});
		}
    }
}
