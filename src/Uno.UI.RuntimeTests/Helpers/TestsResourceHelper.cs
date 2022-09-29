#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Disposables;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

namespace Uno.UI.RuntimeTests.Helpers
{
	internal static class TestsResourceHelper
	{
		private static TestsResources _testsResources;

		/// <summary>
		/// Get resource defined in TestsResources.xaml (templates, styles etc)
		/// </summary>
		public static T GetResource<T>(string resourceName)
		{
			_testsResources ??= new TestsResources();
			return (T)_testsResources[resourceName];
		}
	}
}
