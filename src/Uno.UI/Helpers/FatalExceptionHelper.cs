#nullable enable

using System;

namespace Uno.UI.Helpers;

internal static class FatalExceptionHelper
{
	/// <summary>Returns the runtime-fatal exception wrapped in <paramref name="error"/>, if any.</summary>
	internal static Exception? FindFatalException(Exception error)
	{
		if (error is OutOfMemoryException
			or StackOverflowException
			or AccessViolationException
			or AppDomainUnloadedException
			or BadImageFormatException
			or CannotUnloadAppDomainException)
		{
			return error;
		}

		if (error is AggregateException aggregate)
		{
			foreach (var inner in aggregate.InnerExceptions)
			{
				if (FindFatalException(inner) is { } fatal)
				{
					return fatal;
				}
			}

			return null;
		}

		return error.InnerException is { } innerException
			? FindFatalException(innerException)
			: null;
	}

	internal static bool IsFatal(Exception error) => FindFatalException(error) is not null;
}
