#nullable enable

using System;
using System.Linq;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement
	{
		internal record DebugParseContextDetails(string LocalFileUri, int LineNumber, int LinePosition);

		internal DebugParseContextDetails? DebugParseContext { get; private set; }

		/// <summary>
		/// Sets the base uri for the current element, along with debugging information about the location of the element in the XAML file.
		/// </summary>
		/// <param name="uri">The ms-appx formatted URI</param>
		/// <param name="localFileUri">The file:// path provided for hot reload</param>
		/// <param name="lineNumber">The location of the element in the original file</param>
		/// <param name="linePosition">The location of the element in the original file</param>
		internal void SetBaseUri(string uri, string localFileUri, int lineNumber, int linePosition)
		{
			_baseUriFromParser = uri;

			if (localFileUri is not null)
			{
				DebugParseContext = new DebugParseContextDetails(localFileUri, lineNumber, linePosition);
			}
		}
	}
}
