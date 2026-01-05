namespace Microsoft.VisualStudio.TestTools.UnitTesting;

// Workaround for https://github.com/unoplatform/uno.ui.runtimetests.engine/issues/182
// Do not use this attribute.
[AttributeUsage(AttributeTargets.Method)]
public sealed class ExpectedExceptionAttribute
{
    public Type? ExceptionType => null;
}
