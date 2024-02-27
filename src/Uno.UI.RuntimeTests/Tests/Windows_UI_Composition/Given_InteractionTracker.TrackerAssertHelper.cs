#nullable enable

using System;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

internal partial class Given_InteractionTracker
{
	private class TrackerAssertHelper
	{
		private string[] _logs;
		private int _currentIndex;

		public TrackerAssertHelper(string logs)
		{
			_logs = logs.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
		}

		public bool IsDone => _currentIndex >= _logs.Length;

		public string Current => _logs[_currentIndex];

		public int SkipLines(Predicate<string> predicate, Action<string>? assert = null)
		{
			int countSkip = 0;
			while (!IsDone)
			{
				if (predicate(Current))
				{
					assert?.Invoke(Current);
					countSkip++;
					_currentIndex++;
				}
				else
				{
					break;
				}
			}

			return countSkip;
		}

		public void Advance() => _currentIndex++;
		public void Back() => _currentIndex--;
	}
}
