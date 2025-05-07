// Copyright 2013 The Flutter Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the THIRD-PARTY-NOTICES.md file.

// Ported to C# from https://github.com/flutter/flutter/blob/ea4cdcf39e935bb643b1294abe52c45063597caf/engine/src/flutter/shell/platform/android/io/flutter/plugin/editing/ListenableEditingState.java

using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.Foundation.Logging;
using static System.Net.Mime.MediaTypeNames;

namespace Uno.UI.Runtime.Skia.Android;

internal class ObservableEditingState : SpannableStringBuilder
{
	public delegate void DidChangeEditingStateHandler(bool textChanged, bool selectionChanged, bool composingRegionChanged);

	private int _batchEditNestDepth;

	// We don't support adding/removing listeners, or changing the editing state in a listener
	// callback for now.
	private int _changeNotificationDepth;
	private List<DidChangeEditingStateHandler> _listeners = [];
	private List<DidChangeEditingStateHandler> _pendingListeners = [];
	private List<TextEditingDelta> _batchTextEditingDeltas = [];

	private string? _toStringCache;

	private string? _textWhenBeginBatchEdit;
	private int _selectionStartWhenBeginBatchEdit;
	private int _selectionEndWhenBeginBatchEdit;
	private int _composingStartWhenBeginBatchEdit;
	private int _composingEndWhenBeginBatchEdit;

	private BaseInputConnection _dummyConnection;

	// The View is only used for creating a dummy BaseInputConnection for setComposingRegion. The View
	// needs to have a non-null Context.
	public ObservableEditingState(TextEditState? initialState, global::Android.Views.View view)
	{
		// The View is only used for creating a dummy BaseInputConnection for setComposingRegion. The View
		// needs to have a non-null Context.
		_dummyConnection = new DummyBaseInputConnection(view, true, this);

		if (initialState != null)
		{
			SetEditingState(initialState);
		}
	}

	public List<TextEditingDelta> ExtractBatchTextEditingDeltas()
	{
		var currentBatchDeltas = _batchTextEditingDeltas.ToList();

		_batchTextEditingDeltas.Clear();
		return currentBatchDeltas;
	}

	public void ClearBatchDeltas()
	{
		_batchTextEditingDeltas.Clear();
	}

	/// Starts a new batch edit during which change notifications will be put on hold until all batch
	/// edits end.
	///
	/// Batch edits nest.
	public void BeginBatchEdit()
	{
		_batchEditNestDepth++;
		if (_changeNotificationDepth > 0)
		{
			this.LogDebug()?.Debug("editing state should not be changed in a listener callback");
		}
		if (_batchEditNestDepth == 1 && !_listeners.Empty())
		{
			_textWhenBeginBatchEdit = ToString();
			_selectionStartWhenBeginBatchEdit = SelectionStart;
			_selectionEndWhenBeginBatchEdit = SelectionEnd;
			_composingStartWhenBeginBatchEdit = ComposingStart;
			_composingEndWhenBeginBatchEdit = ComposingEnd;
		}
	}

	/// Ends the current batch edit and flush pending change notifications if the current batch edit
	/// is not nested (i.e. it is the last ongoing batch edit).
	public void EndBatchEdit()
	{
		if (_batchEditNestDepth == 0)
		{
			this.LogDebug()?.Debug($"endBatchEdit called without a matching beginBatchEdit");
			return;
		}

		if (_batchEditNestDepth == 1)
		{
			foreach (DidChangeEditingStateHandler listener in _pendingListeners)
			{
				NotifyListener(listener, true, true, true);
			}

			if (!_listeners.Empty())
			{
				this.LogDebug()?.Debug($"didFinishBatchEdit with {_listeners.Count} listeners");

				bool textChanged = !ToString().Equals(_textWhenBeginBatchEdit);
				bool selectionChanged =
					_selectionStartWhenBeginBatchEdit != SelectionStart
						|| _selectionEndWhenBeginBatchEdit != SelectionEnd;
				bool composingRegionChanged =
					_composingStartWhenBeginBatchEdit != ComposingStart
						|| _composingEndWhenBeginBatchEdit != ComposingEnd;

				NotifyListenersIfNeeded(textChanged, selectionChanged, composingRegionChanged);
			}
		}

		_listeners.AddRange(_pendingListeners);
		_pendingListeners.Clear();
		_batchEditNestDepth--;
	}

	/// Update the composing region of the current editing state.
	///
	/// If the range is invalid or empty, the current composing region will be removed.
	public void SetComposingRange(int composingStart, int composingEnd)
	{
		if (composingStart < 0 || composingStart >= composingEnd)
		{
			BaseInputConnection.RemoveComposingSpans(this);
		}
		else
		{
			_dummyConnection.SetComposingRegion(composingStart, composingEnd);
		}
	}

	/// Called when the framework sends updates to the text input plugin.
	///
	/// This method will also update the composing region if it has changed.
	public void SetEditingState(TextEditState newState)
	{
		BeginBatchEdit();
		Replace(0, Length(), newState.text);

		if (newState.HasSelection)
		{
			Selection.SetSelection(this, newState.selectionStart, newState.selectionEnd);
		}
		else
		{
			Selection.RemoveSelection(this);
		}

		SetComposingRange(newState.composingStart, newState.composingEnd);

		// Updates from the framework should not have a delta created for it as they have already been
		// applied on the framework side.
		ClearBatchDeltas();

		EndBatchEdit();
	}

	public void AddEditingStateListener(DidChangeEditingStateHandler handler)
	{
		if (_changeNotificationDepth > 0)
		{
			this.LogDebug()?.Debug("adding a listener " + handler + " in a listener callback");
		}
		// It is possible for a listener to get added during a batch edit. When that happens we always
		// notify the new listeners.
		// This does not check if the listener is already in the list of existing listeners.
		if (_batchEditNestDepth > 0)
		{
			this.LogDebug()?.Debug("a listener was added to EditingState while a batch edit was in progress");
			_pendingListeners.Add(handler);
		}
		else
		{
			_listeners.Add(handler);
		}
	}

	public void RemoveEditingStateListener(DidChangeEditingStateHandler listener)
	{
		if (_changeNotificationDepth > 0)
		{
			this.LogDebug()?.Debug("removing a listener " + listener + " in a listener callback");
		}
		_listeners.Remove(listener);
		if (_batchEditNestDepth > 0)
		{
			_pendingListeners.Remove(listener);
		}
	}

	public override IEditable? Replace(
	  int start, int end, Java.Lang.ICharSequence? tb, int tbstart, int tbend)
	{
		if (_changeNotificationDepth > 0)
		{
			this.LogDebug()?.Debug("editing state should not be changed in a listener callback");
		}

		string oldText = ToString();

		bool textChanged = end - start != tbend - tbstart;

		if (tb is not null)
		{
			for (int i = 0; i < end - start && !textChanged; i++)
			{
				textChanged |= CharAt(start + i) != tb.CharAt(tbstart + i);
			}
		}

		if (textChanged)
		{
			_toStringCache = null;
		}

		int selectionStart = SelectionStart;
		int selectionEnd = SelectionEnd;
		int composingStart = ComposingStart;
		int composingEnd = ComposingEnd;

		var editable = base.Replace(start, end, tb, tbstart, tbend);

		_batchTextEditingDeltas.Add(
			new TextEditingDelta(
				oldText,
				start,
				end,
				tb?.ToString() ?? "",
				SelectionStart,
				SelectionEnd,
				ComposingStart,
				ComposingEnd));

		if (_batchEditNestDepth > 0)
		{
			return editable;
		}

		bool selectionChanged =
			SelectionStart != selectionStart || SelectionEnd != selectionEnd;
		bool composingRegionChanged =
			ComposingStart != composingStart || ComposingEnd != composingEnd;
		NotifyListenersIfNeeded(textChanged, selectionChanged, composingRegionChanged);

		return editable;
	}

	private void NotifyListener(
		DidChangeEditingStateHandler listener,
		bool textChanged,
		bool selectionChanged,
		bool composingChanged)
	{
		_changeNotificationDepth++;
		listener(textChanged, selectionChanged, composingChanged);
		_changeNotificationDepth--;
	}

	private void NotifyListenersIfNeeded(
		bool textChanged, bool selectionChanged, bool composingChanged)
	{
		if (textChanged || selectionChanged || composingChanged)
		{
			foreach (DidChangeEditingStateHandler listener in _listeners)
			{
				NotifyListener(listener, textChanged, selectionChanged, composingChanged);
			}
		}
	}

	public int SelectionStart => Selection.GetSelectionStart(this);

	public int SelectionEnd => Selection.GetSelectionEnd(this);

	public int ComposingStart => BaseInputConnection.GetComposingSpanStart(this);

	public int ComposingEnd => BaseInputConnection.GetComposingSpanEnd(this);

	public override void SetSpan(Java.Lang.Object? what, int start, int end, SpanTypes flags)
	{
		base.SetSpan(what, start, end, flags);

		// Setting a span does not involve mutating the text value in the editing state. Here we create
		// a non text update delta with any updated selection and composing regions.
		_batchTextEditingDeltas.Add(
			new TextEditingDelta(
				ToString(),
				SelectionStart,
				SelectionEnd,
				ComposingStart,
				ComposingEnd));
	}

	public override string ToString()
		=> _toStringCache != null ? _toStringCache : (_toStringCache = base.ToString());

	private class DummyBaseInputConnection : BaseInputConnection
	{
		private ObservableEditingState _observableEditingState;

		public DummyBaseInputConnection(View targetView, bool fullEditor, ObservableEditingState observableEditingState)
			: base(targetView, fullEditor)
		{
			_observableEditingState = observableEditingState;
		}

		public override IEditable? Editable => _observableEditingState;
	}
}
