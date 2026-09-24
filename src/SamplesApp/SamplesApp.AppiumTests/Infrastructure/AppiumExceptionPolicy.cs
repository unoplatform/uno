#nullable enable

using System;
using System.Threading;

namespace SamplesApp.AppiumTests.Infrastructure;

internal static class AppiumExceptionPolicy
{
	internal static bool IsCritical(Exception exception)
		=> exception is OutOfMemoryException or StackOverflowException or AccessViolationException
			or AppDomainUnloadedException or BadImageFormatException or CannotUnloadAppDomainException
			or InvalidProgramException or ThreadAbortException;
}
