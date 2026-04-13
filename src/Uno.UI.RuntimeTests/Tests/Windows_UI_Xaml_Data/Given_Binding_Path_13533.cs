using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data
{
	[TestClass]
	public class Given_Binding_Path_13533
	{
		// Reproduction for https://github.com/unoplatform/uno/issues/13533
		// On WinUI, setting Binding.Path = new PropertyPath(string.Empty)
		// causes Binding.Path to read back as null. On Skia/GTK, Uno keeps
		// the empty PropertyPath and Binding.Path.Path == "".
		[TestMethod]
		[RunsOnUIThread]
		public void When_Binding_Path_Set_To_Empty_PropertyPath_13533()
		{
			var binding = new Binding { Path = new PropertyPath(string.Empty) };

			Assert.IsNull(
				binding.Path,
				"WinUI returns null for Binding.Path when set with new PropertyPath(string.Empty). " +
				"See https://github.com/unoplatform/uno/issues/13533");
		}
	}
}
