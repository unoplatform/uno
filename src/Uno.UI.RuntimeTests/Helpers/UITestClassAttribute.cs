using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests;

/// <summary>
/// Stands in for the unit tests' [UITestClass] in the files shared with them (UnitTestsImport.props):
/// runtime tests already run on the app's UI thread.
/// </summary>
internal sealed class UITestClassAttribute : TestClassAttribute
{
}
