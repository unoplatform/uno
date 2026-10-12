#nullable enable

using System;
using System.Diagnostics;

namespace Microsoft.UI.Text
{
	internal readonly record struct FormattingStateCloneCounts(
		int Character,
		int Paragraph);

	internal static class FormattingStateCloneDiagnostics
	{
		[ThreadStatic]
		private static bool _isTracking;

		[ThreadStatic]
		private static int _characterClones;

		[ThreadStatic]
		private static int _paragraphClones;

		internal static void BeginTracking()
		{
			_characterClones = 0;
			_paragraphClones = 0;
			_isTracking = true;
		}

		internal static FormattingStateCloneCounts EndTracking()
		{
			_isTracking = false;
			return new(_characterClones, _paragraphClones);
		}

		[Conditional("DEBUG")]
		internal static void RecordCharacterClone()
		{
			if (_isTracking)
			{
				_characterClones++;
			}
		}

		[Conditional("DEBUG")]
		internal static void RecordParagraphClone()
		{
			if (_isTracking)
			{
				_paragraphClones++;
			}
		}
	}
}
