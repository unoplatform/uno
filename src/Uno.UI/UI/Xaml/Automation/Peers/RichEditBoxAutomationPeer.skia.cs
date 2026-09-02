#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class RichEditBoxAutomationPeer
{
	internal static AutomationProperty IsSpellCheckEnabledProperty { get; } = new();

	private List<RichEditBoxTextObjectAutomationPeer> _textObjectPeers = new();
	private HashSet<RichEditBoxTextObjectAutomationPeer> _textObjectPeerSet = new();
	private bool _textObjectCacheInitialized;
	private long _textObjectCacheVersion = -1;
	private int _textObjectIdentityLookupCount;
	private List<RichEditBoxSpellingErrorAutomationPeer> _spellingErrorPeers = new();
	private HashSet<RichEditBoxSpellingErrorAutomationPeer> _spellingErrorPeerSet = new();
	private bool _spellingErrorCacheInitialized;

	private readonly record struct TextObjectKey(
		RichEditTextObjectKind Kind,
		RichEditTextObjectIdentity Identity);

	private readonly record struct SpellingErrorKey(
		int Start,
		int End,
		string Text);

	internal int TextObjectIdentityLookupCount => _textObjectIdentityLookupCount;

	private IList<AutomationPeer> GetTextObjectChildrenCore()
	{
		RefreshAccessibilityPeers();
		if (_textObjectPeers.Count == 0 && _spellingErrorPeers.Count == 0)
		{
			return Array.Empty<AutomationPeer>();
		}

		var children = new List<AutomationPeer>(_textObjectPeers.Count + _spellingErrorPeers.Count);
		children.AddRange(_textObjectPeers);
		children.AddRange(_spellingErrorPeers);
		return children;
	}

	private bool RefreshAccessibilityPeers()
		=> RefreshTextObjectPeers() | RefreshSpellingErrorPeers();

	internal IRawElementProviderSimple[] GetSpellingErrorAnnotations(int start, int end)
	{
		RefreshAccessibilityPeers();
		if (_spellingErrorPeers.Count == 0)
		{
			return Array.Empty<IRawElementProviderSimple>();
		}

		var annotations = new List<IRawElementProviderSimple>();
		foreach (var peer in _spellingErrorPeers)
		{
			var info = peer.Info;
			var intersects = start == end
				? info.Start <= start && start < info.End
				: info.Start < end && info.End > start;
			if (intersects)
			{
				annotations.Add(new IRawElementProviderSimple(peer));
			}
		}

		return annotations.Count == 0
			? Array.Empty<IRawElementProviderSimple>()
			: annotations.ToArray();
	}

	internal IRawElementProviderSimple[] GetTextObjectChildren(int start, int end)
	{
		RefreshAccessibilityPeers();
		if (_textObjectPeers.Count == 0)
		{
			return Array.Empty<IRawElementProviderSimple>();
		}

		var children = new List<IRawElementProviderSimple>();
		foreach (var peer in _textObjectPeers)
		{
			var info = peer.Info;
			var intersects = start == end
				? info.Start <= start && start < info.End
				: info.Start < end && info.End > start;
			if (intersects)
			{
				children.Add(new IRawElementProviderSimple(peer));
			}
		}

		return children.ToArray();
	}

	internal bool TryGetTextObjectRange(AutomationPeer child, out int start, out int end)
	{
		RefreshAccessibilityPeers();
		if (child is RichEditBoxTextObjectAutomationPeer textObjectPeer
			&& _textObjectPeerSet.Contains(textObjectPeer))
		{
			return textObjectPeer.TryGetRange(out start, out end);
		}

		start = 0;
		end = 0;
		return false;
	}

	internal bool TryGetSpellingAnnotationRange(AutomationPeer child, out int start, out int end)
	{
		RefreshAccessibilityPeers();
		if (child is RichEditBoxSpellingErrorAutomationPeer spellingErrorPeer
			&& _spellingErrorPeerSet.Contains(spellingErrorPeer))
		{
			return spellingErrorPeer.TryGetRange(out start, out end);
		}

		start = 0;
		end = 0;
		return false;
	}

	internal bool TryGetEnclosingTextObject(int start, int end, out RichEditBoxTextObjectAutomationPeer textObject)
	{
		RefreshAccessibilityPeers();
		RichEditBoxTextObjectAutomationPeer? bestMatch = null;
		var bestLength = int.MaxValue;
		foreach (var peer in _textObjectPeers)
		{
			var info = peer.Info;
			var contains = start == end
				? info.Start <= start && start < info.End
				: info.Start <= start && end <= info.End;
			var length = info.End - info.Start;
			if (contains && length < bestLength)
			{
				bestMatch = peer;
				bestLength = length;
			}
		}

		textObject = bestMatch!;
		return bestMatch is not null;
	}

	internal void OnDocumentAccessibilityChanged()
	{
		var structureChanged = RefreshAccessibilityPeers();
		InvalidatePeer();
		if (structureChanged && ListenerExistsHelper(AutomationEvents.StructureChanged))
		{
			RaiseAutomationEvent(AutomationEvents.StructureChanged);
		}
	}

	internal void RaiseIsSpellCheckEnabledPropertyChangedEvent(bool oldValue, bool newValue)
		=> RaisePropertyChangedEvent(IsSpellCheckEnabledProperty, oldValue, newValue);

	internal void RaisePlatformTextEditTextChangedEvent(
		AutomationTextEditChangeType changeType,
		IReadOnlyList<string> changedData)
	{
		var listener = AutomationPeerListener;
		if (listener is ITextEditAutomationPeerListener textEditListener
			&& listener.ListenerExistsHelper(AutomationEvents.TextEditTextChanged))
		{
			textEditListener.NotifyTextEditTextChangedEvent(this, changeType, changedData);
		}
	}

	private bool RefreshTextObjectPeers()
	{
		if (Owner is not RichEditBox owner)
		{
			foreach (var peer in _textObjectPeers)
			{
				peer.Invalidate();
			}
			_textObjectPeers.Clear();
			_textObjectPeerSet.Clear();
			_textObjectCacheInitialized = true;
			return true;
		}

		var version = owner.Document.AutomationVersion;
		if (_textObjectCacheInitialized && _textObjectCacheVersion == version)
		{
			return false;
		}

		var objects = owner.Document.GetAutomationTextObjects();
		_textObjectIdentityLookupCount = 0;
		var unmatched = new Dictionary<TextObjectKey, Queue<RichEditBoxTextObjectAutomationPeer>>();
		foreach (var peer in _textObjectPeers)
		{
			var key = new TextObjectKey(peer.Info.Kind, peer.Info.Identity);
			if (!unmatched.TryGetValue(key, out var queue))
			{
				queue = new Queue<RichEditBoxTextObjectAutomationPeer>();
				unmatched.Add(key, queue);
			}
			queue.Enqueue(peer);
		}

		var next = new List<RichEditBoxTextObjectAutomationPeer>(objects.Count);
		var structureChanged = !_textObjectCacheInitialized && objects.Count != 0;
		foreach (var info in objects)
		{
			_textObjectIdentityLookupCount++;
			RichEditBoxTextObjectAutomationPeer? match = null;
			var key = new TextObjectKey(info.Kind, info.Identity);
			if (unmatched.TryGetValue(key, out var queue) && queue.Count > 0)
			{
				match = queue.Dequeue();
				if (queue.Count == 0)
				{
					unmatched.Remove(key);
				}
			}

			if (match is null)
			{
				match = info.Kind switch
				{
					RichEditTextObjectKind.Link => new RichEditBoxLinkAutomationPeer(owner, info),
					RichEditTextObjectKind.Image => new RichEditBoxImageAutomationPeer(owner, info),
					_ => throw new InvalidOperationException($"Unsupported RichEditBox text object kind: {info.Kind}."),
				};
				match.SetParent(this);
				structureChanged = true;
			}
			else
			{
				match.Update(info);
			}

			next.Add(match);
		}

		if (unmatched.Count != 0)
		{
			structureChanged = true;
			foreach (var queue in unmatched.Values)
			{
				while (queue.Count > 0)
				{
					var stale = queue.Dequeue();
					stale.Invalidate();
					stale.SetParent(null);
				}
			}
		}

		if (!structureChanged && _textObjectPeers.Count == next.Count)
		{
			for (var i = 0; i < next.Count; i++)
			{
				if (!ReferenceEquals(_textObjectPeers[i], next[i]))
				{
					structureChanged = true;
					break;
				}
			}
		}

		_textObjectPeers = next;
		_textObjectPeerSet = new HashSet<RichEditBoxTextObjectAutomationPeer>(next);
		_textObjectCacheInitialized = true;
		_textObjectCacheVersion = version;
		foreach (var peer in next)
		{
			peer.RaisePendingPropertyChanges();
		}
		return structureChanged;
	}

	private bool RefreshSpellingErrorPeers()
	{
		if (Owner is not RichEditBox owner)
		{
			foreach (var peer in _spellingErrorPeers)
			{
				peer.Invalidate();
				peer.SetParent(null);
			}
			var hadPeers = _spellingErrorPeers.Count != 0 || !_spellingErrorCacheInitialized;
			_spellingErrorPeers.Clear();
			_spellingErrorPeerSet.Clear();
			_spellingErrorCacheInitialized = true;
			return hadPeers;
		}

		var annotations = owner.GetAccessibilitySpellingAnnotations();
		var unmatched = new Dictionary<SpellingErrorKey, Queue<RichEditBoxSpellingErrorAutomationPeer>>();
		foreach (var peer in _spellingErrorPeers)
		{
			var info = peer.Info;
			var key = new SpellingErrorKey(info.Start, info.End, info.Text);
			if (!unmatched.TryGetValue(key, out var queue))
			{
				queue = new Queue<RichEditBoxSpellingErrorAutomationPeer>();
				unmatched.Add(key, queue);
			}
			queue.Enqueue(peer);
		}

		var next = new List<RichEditBoxSpellingErrorAutomationPeer>(annotations.Count);
		var structureChanged = !_spellingErrorCacheInitialized && annotations.Count != 0;
		foreach (var info in annotations)
		{
			RichEditBoxSpellingErrorAutomationPeer? match = null;
			var key = new SpellingErrorKey(info.Start, info.End, info.Text);
			if (unmatched.TryGetValue(key, out var queue) && queue.Count > 0)
			{
				match = queue.Dequeue();
				if (queue.Count == 0)
				{
					unmatched.Remove(key);
				}
			}

			if (match is null)
			{
				match = new RichEditBoxSpellingErrorAutomationPeer(owner, info);
				match.SetParent(this);
				structureChanged = true;
			}
			else
			{
				match.Update(info);
			}

			next.Add(match);
		}

		if (unmatched.Count != 0)
		{
			structureChanged = true;
			foreach (var queue in unmatched.Values)
			{
				while (queue.Count > 0)
				{
					var stale = queue.Dequeue();
					stale.Invalidate();
					stale.SetParent(null);
				}
			}
		}

		if (!structureChanged && _spellingErrorPeers.Count == next.Count)
		{
			for (var i = 0; i < next.Count; i++)
			{
				if (!ReferenceEquals(_spellingErrorPeers[i], next[i]))
				{
					structureChanged = true;
					break;
				}
			}
		}

		_spellingErrorPeers = next;
		_spellingErrorPeerSet = new HashSet<RichEditBoxSpellingErrorAutomationPeer>(next);
		_spellingErrorCacheInitialized = true;
		return structureChanged;
	}
}
