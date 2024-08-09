using System;
using System.Globalization;
using System.Reflection;
using Uno.UI.RemoteControl.HotReload.Messages;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.HotReload
{
	internal static class FrameworkElementExtensions
	{
		private static (string FileName, int FileLine, int LinePosition) GetDebugParseContext(this FrameworkElement element)
		{
			var dpcProp = typeof(FrameworkElement).GetProperty("DebugParseContext", BindingFlags.Instance | BindingFlags.NonPublic);

			if (dpcProp == null)
			{
				return (string.Empty, -1, -1);
			}

			var dpcForElement = dpcProp.GetValue(element);

			if (dpcForElement is null)
			{
				return (string.Empty, -1, -1);
			}

			var fl = dpcForElement.GetType().GetProperties();

			if (fl is null)
			{
				return (string.Empty, -1, -1);
			}

			var fileName = fl[0].GetValue(dpcForElement)?.ToString() ?? string.Empty;

			// Don't return details for embedded controls.
			if (fileName.StartsWith("ms-appx:///Uno.UI/", StringComparison.InvariantCultureIgnoreCase)
				|| fileName.EndsWith("mergedstyles.xaml", StringComparison.InvariantCultureIgnoreCase))
			{
				return (string.Empty, -1, -1);
			}

			_ = int.TryParse(fl[1].GetValue(dpcForElement)?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int line);
			_ = int.TryParse(fl[2].GetValue(dpcForElement)?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int pos);

			const string FileTypePrefix = "file:///";

			// Strip any file protocol prefix as not expected by the server
			if (fileName.StartsWith(FileTypePrefix, StringComparison.InvariantCultureIgnoreCase))
			{
				fileName = fileName.Substring(FileTypePrefix.Length);
			}

			return (fileName, line, pos);
		}

		public static UpdateFile CreateUpdateFileMessage(
			this FrameworkElement element,
			string originalText,
			string replacementText)
		{
			var fileInfo = element.GetDebugParseContext();
			return new UpdateFile
			{
				FilePath = fileInfo.FileName,
				OldText = originalText,
				NewText = replacementText
			};

		}
	}
}
