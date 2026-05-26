#if !WINAPPSDK
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data
{
	[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
	public class Given_Convert
	{

		[TestMethod]
		public void When_Uri()
		{
			string Expected = "http://platform.uno";

			var converter = GetConverter(typeof(Uri));
			Assert.AreEqual(Expected, converter.ConvertTo(new Uri("http://platform.uno"), typeof(string)));

			// TODO? TypeDescriptor.GetConverter(Type) has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code.
			[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "TODO")]
			[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "TODO")]
			static TypeConverter GetConverter(Type type) => TypeDescriptor.GetConverter(type);
		}
	}
}
#endif
