using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Testing;

/// <summary>
/// A test failure the runtime-test harness reports straight away instead of retrying.
/// </summary>
/// <remarks>
/// Use it for failures that would cost the same wait on every attempt, such as a child process that was
/// killed after outliving its budget. Ordinary assertion failures should keep using <see cref="Assert"/>.
/// </remarks>
public class NonRetryableTestFailureException : AssertFailedException
{
	public NonRetryableTestFailureException(string message) : base(message)
	{
	}

	public NonRetryableTestFailureException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
