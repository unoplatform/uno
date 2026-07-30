using System.Runtime.Loader;

namespace Uno.UI
{
	public static partial class ResourceResolver
	{
		/// <summary>
		/// Schedules removal of an ALC's scoped resource-dictionary registrations once that ALC
		/// unloads. The <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/> values hold
		/// <see cref="System.Func{TResult}"/>s over generated code in that ALC, so while the entry
		/// lives the ALC's LoaderAllocator can't be collected — and during unload the runtime keeps
		/// the ALC (the live key) rooted until the LoaderAllocator dies, pinning it forever.
		/// Teardown cleanup runs BEFORE <c>Unload()</c> is initiated, so removal is driven by the
		/// <see cref="AssemblyLoadContext.Unloading"/> event (subscribed once, when the ALC first
		/// registers a scoped dictionary) to fire at the right moment.
		/// </summary>
		private static void ScheduleAlcScopedRegistrationCleanup(AssemblyLoadContext alc)
		{
			alc.Unloading += static unloading =>
			{
				lock (_alcDictionariesLock)
				{
					_registeredDictionariesByUriByAlc.Remove(unloading);
				}
			};
		}
	}
}
