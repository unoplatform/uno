#if !WINAPPSDK
using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Helpers;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Uno_Helpers
{
	/// <summary>
	/// Tests are based on <see href="https://docs.microsoft.com/en-us/windows/uwp/app-resources/uri-schemes#ms-appdata" />.
	/// </summary>
	[TestClass]
	public class Given_AppDataUriEvaluator
	{
		[TestMethod]
		public void When_Uri_Is_Null()
		{
			Assert.ThrowsExactly<ArgumentNullException>(
				() => AppDataUriEvaluator.ToPath(null));
		}

		[TestMethod]
		public void When_Uri_Has_Different_Scheme()
		{
			var uri = new Uri("ms-appx:///local/test.txt");

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(
				() => AppDataUriEvaluator.ToPath(uri));
		}

		[TestMethod]
		public void When_Uri_Is_Not_Absolute()
		{
			var uri = new Uri("local/test.txt", UriKind.Relative);

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(
				() => AppDataUriEvaluator.ToPath(uri));
		}

		[TestMethod]
		public void When_Uri_Has_No_Path()
		{
			var uri = new Uri("ms-appdata:");

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(
				() => AppDataUriEvaluator.ToPath(uri));
		}

		[TestMethod]
		public void When_Uri_Has_No_Subfolder_Specification()
		{
			var uri = new Uri("ms-appdata:///");

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(
				() => AppDataUriEvaluator.ToPath(uri));
		}

		[TestMethod]
		public void When_Uri_Is_LocalFolder()
		{
			var uri = new Uri("ms-appdata:///local/");

			var path = AppDataUriEvaluator.ToPath(uri);

			Assert.AreEqual(ApplicationData.Current.LocalFolder.Path, path);
		}

		[TestMethod]
		public void When_Uri_Is_LocalFolder_Without_Trailing_Slash()
		{
			var uri = new Uri("ms-appdata:///local");

			var path = AppDataUriEvaluator.ToPath(uri);

			Assert.AreEqual(ApplicationData.Current.LocalFolder.Path, path);
		}

		[TestMethod]
		public void When_Uri_Is_RoamingFolder()
		{
			var uri = new Uri("ms-appdata:///roaming/");

			var path = AppDataUriEvaluator.ToPath(uri);

			Assert.AreEqual(ApplicationData.Current.RoamingFolder.Path, path);
		}

		[TestMethod]
		public void When_Uri_Is_RoamingFolder_But_Has_Suffix()
		{
			var uri = new Uri("ms-appdata:///roamingfolder/");

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(
				() => AppDataUriEvaluator.ToPath(uri));
		}

		[TestMethod]
		public void When_Uri_Is_CacheFolder()
		{
			var uri = new Uri("ms-appdata:///temp/");

			var path = AppDataUriEvaluator.ToPath(uri);

			Assert.AreEqual(ApplicationData.Current.TemporaryFolder.Path, path);
		}

		[TestMethod]
		public void When_Uri_Starts_With_Invalid_Folder()
		{
			var uri = new Uri($"ms-appdata:///space/file.png");

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(
				() => AppDataUriEvaluator.ToPath(uri));
		}

		[TestMethod]
		public void When_Uri_Is_File_Nested_In_LocalFolder()
		{
			var subpath = "path/tO/my/FilE123.txt";
			var uri = new Uri($"ms-appdata:///local/{subpath}");
			var expectedPath = Path.GetFullPath(Path.Combine(ApplicationData.Current.LocalFolder.Path, subpath));

			var path = AppDataUriEvaluator.ToPath(uri);

			Assert.AreEqual(expectedPath, path);
		}

		[TestMethod]
		public void When_Uri_Contains_Package_Name()
		{
			var subpath = "test/file.png";
			var packageName = Package.Current.Id.Name;
			var uri = new Uri($"ms-appdata://{packageName}/local/{subpath}");
			var expected = Path.GetFullPath(Path.Combine(ApplicationData.Current.LocalFolder.Path, subpath));

			var path = AppDataUriEvaluator.ToPath(uri);

			Assert.AreEqual(expected, path);
		}

		[TestMethod]
		public void When_Uri_Contains_Package_Name_But_Has_Suffix()
		{
			var packageName = Package.Current.Id.Name;
			var uri = new Uri($"ms-appdata://{packageName}abcd/local/file.png");

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(
				() => AppDataUriEvaluator.ToPath(uri));
		}

		[TestMethod]
		public void When_Dot_In_Uri()
		{
			var subpath = "test/./file.png";
			var shortenedSubpath = "test/file.png";

			var uri = new Uri($"ms-appdata:///local/{subpath}");
			var expected = Path.GetFullPath(Path.Combine(ApplicationData.Current.LocalFolder.Path, shortenedSubpath));

			var path = AppDataUriEvaluator.ToPath(uri);

			Assert.AreEqual(expected, path);
		}

		[TestMethod]
		public void When_Double_Dot_In_Scope_Of_LocalFolder()
		{
			var subpath = "test/ok/../file.png";
			var shortenedSubpath = "test/file.png";

			var uri = new Uri($"ms-appdata:///local/{subpath}");
			var expected = Path.GetFullPath(Path.Combine(ApplicationData.Current.LocalFolder.Path, shortenedSubpath));

			var path = AppDataUriEvaluator.ToPath(uri);

			Assert.AreEqual(expected, path);
		}

		[TestMethod]
		public void When_Double_Dot_Switch_To_Different_Root_Folder()
		{
			var uri = new Uri("ms-appdata:///local/../roaming/test.png");
			var expected = Path.GetFullPath(Path.Combine(ApplicationData.Current.RoamingFolder.Path, "test.png"));

			var path = AppDataUriEvaluator.ToPath(uri);

			Assert.AreEqual(expected, path);
		}

		[TestMethod]
		public void When_Double_Dot_Backs_Out_Above_App_Data()
		{
			var uri = new Uri("ms-appdata:///local/../hello/logo.png");

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(
				() => AppDataUriEvaluator.ToPath(uri));
		}

		[TestMethod]
		public void When_Uri_Contains_Weird_Chars()
		{
			var uri = new Uri("ms-appdata:///local/Hello%23World.html");
			var expected = Path.Combine(ApplicationData.Current.LocalFolder.Path, "Hello#World.html");

			var path = AppDataUriEvaluator.ToPath(uri);

			Assert.AreEqual(expected, path);
		}

		[TestMethod]
		public void When_Uri_Contains_Plus_Sign()
		{
			var uri = new Uri("ms-appdata:///local/+%23World.html");
			var expected = Path.Combine(ApplicationData.Current.LocalFolder.Path, "+#World.html");

			var path = AppDataUriEvaluator.ToPath(uri);

			Assert.AreEqual(expected, path);
		}

		[TestMethod]
		public void When_NonExistent_Method_Check()
		{
			Assert.IsFalse(ApiInformation.IsMethodPresent("Microsoft.UI.Composition.Compositor, Uno.UI.Composition", "IDontExist"));
		}
	}
}
#endif
