namespace Microsoft.VisualStudio.TestTools.UnitTesting;

// Workaround for https://github.com/unoplatform/uno.ui.runtimetests.engine/issues/182
// Do not use this attribute.
// Intentionally not inheriting from Attribute to prevent accidental use of the class.
// This only provides the minimal API so that runtimetests.engine can work.
public sealed class ExpectedExceptionAttribute
{
    public Type? ExceptionType => null;
}
