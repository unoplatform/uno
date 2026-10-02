#nullable enable

using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml.FrameworkElementTests;

[TestClass]
public class Given_DataContext_Nullability
{
	// WinUI ships no nullable annotations, so DataContext must stay oblivious.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24875")]
	public void When_DataContext_Is_Nullable_Oblivious()
	{
		PropertyInfo property = typeof(FrameworkElement).GetProperty(nameof(FrameworkElement.DataContext))!;
		NullabilityInfoContext context = new();

		var info = context.Create(property);

		Assert.AreEqual(NullabilityState.Unknown, info.ReadState);
		Assert.AreEqual(NullabilityState.Unknown, info.WriteState);
	}
}
