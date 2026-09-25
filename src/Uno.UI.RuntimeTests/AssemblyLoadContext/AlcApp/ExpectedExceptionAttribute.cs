using System;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

// Workaround for https://github.com/unoplatform/uno.ui.runtimetests.engine/issues/182
// Do not use this attribute.
// This only provides the minimal API so that runtimetests.engine can work.
public sealed class ExpectedExceptionAttribute : Attribute
{
    public Type? ExceptionType => null;
}
