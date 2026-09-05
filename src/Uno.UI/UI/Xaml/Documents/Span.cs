using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Uno.Extensions.Specialized;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Documents
{
	[ContentProperty(Name = nameof(Inlines))]
	public partial class Span : Inline
	{
		public Span()
		{
			Inlines = new InlineCollection(this);
		}

		public InlineCollection Inlines { get; set; }

		// MUX Reference Span::AppendAutomationPeerChildren — if this Span exposes a peer (e.g. Hyperlink),
		// append it; otherwise recurse into the inlines whose content-start falls within [startPos, endPos].
		// The position-filtered walk needs the TextPointer/position layer, which only Skia has; the override
		// still has to exist everywhere so the reference assembly matches the runtime API.
		internal protected override void AppendAutomationPeerChildren(IList<AutomationPeer> automationPeerChildren, int startPos, int endPos)
		{
#if __SKIA__
			var automationPeer = GetOrCreateAutomationPeer();
			if (automationPeer is not null)
			{
				automationPeerChildren.Add(automationPeer);
			}
			else
			{
				foreach (var inline in Inlines)
				{
					var inlineStart = inline.GetContentStart();
					var posInlineStart = inlineStart?.Offset ?? -1;
					if (startPos <= posInlineStart && posInlineStart <= endPos)
					{
						inline.AppendAutomationPeerChildren(automationPeerChildren, startPos, endPos);
					}
				}
			}
#else
			base.AppendAutomationPeerChildren(automationPeerChildren, startPos, endPos);
#endif
		}
	}
}
