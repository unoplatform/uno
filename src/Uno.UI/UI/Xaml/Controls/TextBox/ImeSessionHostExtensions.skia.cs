#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

internal static class ImeSessionHostExtensions
{
	internal static bool TryGetCandidateWindowRect(this IImeSessionHost host, out Rect rect)
	{
		if (host.TextBoxView?.DisplayBlock is not { ParsedText: { } parsedText } displayBlock
			|| host.XamlRoot is null)
		{
			rect = Rect.Empty;
			return false;
		}

		var caret = host.IsBackwardSelection
			? host.SelectionStart
			: host.SelectionStart + host.SelectionLength;
		var caretRect = parsedText.GetRectForIndex(caret);
		var candidateTop = host.DesiredCandidateWindowAlignment == CandidateWindowAlignment.BottomEdge
			? displayBlock.ActualHeight
			: caretRect.Top;
		var candidateHeight = host.DesiredCandidateWindowAlignment == CandidateWindowAlignment.BottomEdge
			? 1
			: caretRect.Height;
		var transform = displayBlock.TransformToVisual(null);
		var top = transform.TransformPoint(new Point(caretRect.Left, candidateTop));
		var bottom = transform.TransformPoint(new Point(caretRect.Left, candidateTop + candidateHeight));
		rect = new Rect(top.X, top.Y, 1, Math.Max(1, bottom.Y - top.Y));
		return true;
	}
}
