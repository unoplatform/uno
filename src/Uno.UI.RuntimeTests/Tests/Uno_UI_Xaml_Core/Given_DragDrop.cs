using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Core;

[TestClass]
public class Given_DragDrop
{
#if HAS_UNO // InputManager is internal.
	[TestMethod]
	[RunsOnUIThread]
	public void Verify_Initialized()
	{
		var xamlRoot = TestServices.WindowHelper.XamlRoot;
		var contentRoot = xamlRoot.VisualTree.ContentRoot;
		Assert.IsNotNull(contentRoot.InputManager.DragDrop);
	}
#endif
}
