#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.Ime;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Win32 IMM32-based implementation of <see cref="IImeTextBoxExtension"/>.
/// Handles WM_IME_STARTCOMPOSITION, WM_IME_COMPOSITION, and WM_IME_ENDCOMPOSITION
/// messages to provide IME support for TextBox on Win32 Skia.
/// </summary>
internal sealed class Win32ImeTextBoxExtension : IImeTextBoxExtension
{
	internal static Win32ImeTextBoxExtension Instance { get; } = new();

	private HWND _hwnd;
	private bool _isComposing;
	private string? _pendingResultText;
	private string _lastCompositionText = string.Empty;
	private IImeSessionHost? _activeHost;
	private Point? _lastCandidatePosition;

	private const uint CFS_CANDIDATEPOS = 0x0040;

	private Win32ImeTextBoxExtension()
	{
	}

	public bool IsComposing => _isComposing;

	public event EventHandler? CompositionStarted;
	public event EventHandler<ImeCompositionEventArgs>? CompositionUpdated;
	public event EventHandler<ImeCompositionEventArgs>? CompositionCompleted;
	public event EventHandler<ImePartialCompositionEventArgs>? CompositionPartiallyCommitted;
	public event EventHandler<ImeCompositionEventArgs>? CompositionCanceled;
	public event EventHandler? CompositionEnded;

	public event EventHandler<ImeCandidateWindowBoundsChangedEventArgs>? CandidateWindowBoundsChanged
	{
		add { }
		remove { }
	}

	public void StartImeSession(IImeSessionHost host, ImeSessionActivation activation)
	{
		var wrapper = (Win32WindowWrapper)XamlRootMap.GetHostForRoot(host.XamlRoot!)!;
		_hwnd = (HWND)((Win32NativeWindow)wrapper.NativeWindow!).Hwnd;
		_activeHost = host;
		_lastCandidatePosition = null;
		UpdateCandidateWindowPosition(host);
	}

	public void EndImeSession()
	{
		if (_isComposing)
		{
			// Tell the IME to commit the active composition and close its windows
			var himc = PInvoke.ImmGetContext(_hwnd);
			if (!himc.IsNull)
			{
				PInvoke.ImmNotifyIME(himc, NOTIFY_IME_ACTION.NI_COMPOSITIONSTR, NOTIFY_IME_INDEX.CPS_COMPLETE, 0);
				PInvoke.ImmReleaseContext(_hwnd, himc);
			}

			if (_isComposing)
			{
				CompositionCompleted?.Invoke(
					this,
					new ImeCompositionEventArgs(_pendingResultText ?? _lastCompositionText));
				ResetCompositionState();
				CompositionEnded?.Invoke(this, EventArgs.Empty);
			}
		}

		_hwnd = HWND.Null;
		_activeHost = null;
		_lastCandidatePosition = null;
	}

	public void UpdateImeSession(IImeSessionHost host, ImeSessionUpdate update)
	{
		if ((update & (ImeSessionUpdate.CandidateWindowAlignment | ImeSessionUpdate.TextAndSelection)) != 0)
		{
			UpdateCandidateWindowPosition(host);
		}
	}

	public Task<IReadOnlyList<string>> GetLinguisticAlternativesAsync(string compositionText, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var candidates = GetCandidateList();
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult<IReadOnlyList<string>>(candidates);
	}

	/// <summary>
	/// Called from WndProc when WM_IME_STARTCOMPOSITION is received.
	/// </summary>
	internal void OnWmImeStartComposition()
	{
		if (_activeHost is { } host)
		{
			UpdateCandidateWindowPosition(host);
		}
		_isComposing = true;
		_pendingResultText = null;
		_lastCompositionText = string.Empty;
		CompositionStarted?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Called from WndProc when WM_IME_COMPOSITION is received.
	/// </summary>
	internal unsafe void OnWmImeComposition(LPARAM lParam)
	{
		if (_activeHost is { } host)
		{
			UpdateCandidateWindowPosition(host);
		}
		var himc = PInvoke.ImmGetContext(_hwnd);
		if (himc.IsNull)
		{
			return;
		}

		try
		{
			var flags = (IME_COMPOSITION_STRING)(uint)lParam.Value;
			var hasResult = flags.HasFlag(IME_COMPOSITION_STRING.GCS_RESULTSTR);
			var hasComposition = flags.HasFlag(IME_COMPOSITION_STRING.GCS_COMPSTR);
			var resultText = hasResult
				? GetCompositionString(himc, IME_COMPOSITION_STRING.GCS_RESULTSTR)
				: null;
			var compositionText = hasComposition
				? GetCompositionString(himc, IME_COMPOSITION_STRING.GCS_COMPSTR)
				: null;

			if (hasResult && !string.IsNullOrEmpty(resultText) && !string.IsNullOrEmpty(compositionText))
			{
				ReportPartialResult(himc, resultText, compositionText);
				return;
			}

			if (hasResult && resultText is not null)
			{
				_pendingResultText = resultText;
				return;
			}

			if (hasComposition && compositionText is not null)
			{
				if (_pendingResultText is { } pendingResult && compositionText.Length > 0)
				{
					_pendingResultText = null;
					ReportPartialResult(himc, pendingResult, compositionText);
					return;
				}

				if (_pendingResultText is not null)
				{
					return;
				}

				var cursorPosition = PInvoke.ImmGetCompositionString(himc, IME_COMPOSITION_STRING.GCS_CURSORPOS, null, 0);
				var resolvedLength = GetResolvedLength(himc, compositionText.Length);
				_lastCompositionText = compositionText;
				CompositionUpdated?.Invoke(this, new ImeCompositionEventArgs(compositionText, cursorPosition, resolvedLength));
			}
		}
		finally
		{
			PInvoke.ImmReleaseContext(_hwnd, himc);
		}
	}

	/// <summary>
	/// Called from WndProc when WM_IME_ENDCOMPOSITION is received.
	/// </summary>
	internal void OnWmImeEndComposition()
	{
		if (!_isComposing)
		{
			return;
		}

		if (_pendingResultText is { } resultText)
		{
			CompositionCompleted?.Invoke(this, new ImeCompositionEventArgs(resultText));
		}
		else
		{
			CompositionCanceled?.Invoke(this, new ImeCompositionEventArgs(string.Empty));
		}
		ResetCompositionState();
		CompositionEnded?.Invoke(this, EventArgs.Empty);
	}

	private unsafe void ReportPartialResult(HIMC himc, string resultText, string compositionText)
	{
		var cursorPosition = PInvoke.ImmGetCompositionString(himc, IME_COMPOSITION_STRING.GCS_CURSORPOS, null, 0);
		var resolvedLength = GetResolvedLength(himc, compositionText.Length);
		_lastCompositionText = compositionText;
		_pendingResultText = null;
		CompositionPartiallyCommitted?.Invoke(
			this,
			new ImePartialCompositionEventArgs(
				resultText,
				compositionText,
				cursorPosition,
				resolvedLength));
	}

	private void ResetCompositionState()
	{
		_isComposing = false;
		_pendingResultText = null;
		_lastCompositionText = string.Empty;
	}

	/// <summary>
	/// Counts leading characters in the composition that are already resolved
	/// (ATTR_TARGET_CONVERTED or ATTR_FIXEDCONVERTED) using GCS_COMPATTR.
	/// </summary>
	private static unsafe int GetResolvedLength(HIMC himc, int compositionLength)
	{
		var byteLen = PInvoke.ImmGetCompositionString(himc, IME_COMPOSITION_STRING.GCS_COMPATTR, null, 0);
		if (byteLen <= 0)
		{
			return 0;
		}

		var attrs = stackalloc byte[byteLen];
		var result = PInvoke.ImmGetCompositionString(himc, IME_COMPOSITION_STRING.GCS_COMPATTR, attrs, (uint)byteLen);
		if (result <= 0)
		{
			return 0;
		}

		// ATTR_INPUT = 0, ATTR_TARGET_CONVERTED = 1, ATTR_CONVERTED = 2,
		// ATTR_TARGET_NOTCONVERTED = 3, ATTR_INPUT_ERROR = 4, ATTR_FIXEDCONVERTED = 5
		// Count leading non-input characters (already converted/resolved).
		var count = Math.Min(result, compositionLength);
		for (var i = 0; i < count; i++)
		{
			if (attrs[i] == 0) // ATTR_INPUT
			{
				return i;
			}
		}

		return count;
	}

	private static unsafe string? GetCompositionString(HIMC himc, IME_COMPOSITION_STRING dwIndex)
	{
		var byteLen = PInvoke.ImmGetCompositionString(himc, dwIndex, null, 0);
		if (byteLen <= 0)
		{
			return dwIndex == IME_COMPOSITION_STRING.GCS_COMPSTR ? string.Empty : null;
		}

		var buffer = stackalloc byte[byteLen];
		var result = PInvoke.ImmGetCompositionString(himc, dwIndex, buffer, (uint)byteLen);
		if (result <= 0)
		{
			return null;
		}

		return new string((char*)buffer, 0, result / sizeof(char));
	}

	private unsafe void UpdateCandidateWindowPosition(IImeSessionHost host)
	{
		if (_hwnd.IsNull ||
			host.TextBoxView?.DisplayBlock.ParsedText is not { } parsedText ||
			host.XamlRoot is not { } xamlRoot)
		{
			return;
		}

		var index = host.IsBackwardSelection
			? host.SelectionStart
			: host.SelectionStart + host.SelectionLength;
		var caretRect = parsedText.GetRectForIndex(index);
		var candidateY = host.DesiredCandidateWindowAlignment == CandidateWindowAlignment.BottomEdge
			? host.TextBoxView.DisplayBlock.ActualHeight
			: caretRect.Top;
		var rootPoint = host.TextBoxView.DisplayBlock
			.TransformToVisual(null)
			.TransformPoint(new Windows.Foundation.Point(caretRect.Left, candidateY));
		var scale = xamlRoot.RasterizationScale;
		var candidatePosition = new Point(
			(int)(rootPoint.X * scale),
			(int)(rootPoint.Y * scale));
		if (_lastCandidatePosition == candidatePosition)
		{
			return;
		}

		var himc = PInvoke.ImmGetContext(_hwnd);
		if (himc.IsNull)
		{
			return;
		}

		try
		{
			var candidateForm = new CANDIDATEFORM
			{
				dwIndex = 0,
				dwStyle = CFS_CANDIDATEPOS,
				ptCurrentPos = candidatePosition,
			};
			if (PInvoke.ImmSetCandidateWindow(himc, &candidateForm))
			{
				_lastCandidatePosition = candidatePosition;
			}
		}
		finally
		{
			PInvoke.ImmReleaseContext(_hwnd, himc);
		}
	}

	private unsafe IReadOnlyList<string> GetCandidateList()
	{
		if (!_isComposing || _hwnd.IsNull || _activeHost is null)
		{
			return Array.Empty<string>();
		}

		var himc = PInvoke.ImmGetContext(_hwnd);
		if (himc.IsNull)
		{
			return Array.Empty<string>();
		}

		try
		{
			var byteCount = ImmGetCandidateList(himc, 0, null, 0);
			if (byteCount == 0)
			{
				return Array.Empty<string>();
			}

			var buffer = new byte[byteCount];
			fixed (byte* candidateList = buffer)
			{
				if (ImmGetCandidateList(himc, 0, candidateList, byteCount) == 0)
				{
					return Array.Empty<string>();
				}

				if (byteCount < 24)
				{
					throw new InvalidOperationException("The IME returned an invalid candidate list.");
				}

				var count = *(uint*)(candidateList + 8);
				var maximumOffsetCount = (byteCount - 24) / sizeof(uint);
				if (count > maximumOffsetCount)
				{
					throw new InvalidOperationException("The IME returned an invalid candidate list.");
				}
				var offsets = (uint*)(candidateList + 24);
				var candidates = new string[count];
				for (var i = 0; i < count; i++)
				{
					if (offsets[i] >= byteCount)
					{
						throw new InvalidOperationException("The IME returned an invalid candidate offset.");
					}

					var candidate = (char*)(candidateList + offsets[i]);
					var maximumLength = (int)((byteCount - offsets[i]) / sizeof(char));
					var length = 0;
					while (length < maximumLength && candidate[length] != '\0')
					{
						length++;
					}
					if (length == maximumLength)
					{
						throw new InvalidOperationException("The IME returned an unterminated candidate.");
					}
					candidates[i] = new string(candidate, 0, length);
				}

				return Array.AsReadOnly(candidates);
			}
		}
		finally
		{
			PInvoke.ImmReleaseContext(_hwnd, himc);
		}
	}

	[DllImport("imm32.dll", EntryPoint = "ImmGetCandidateListW", CharSet = CharSet.Unicode)]
	private static extern unsafe uint ImmGetCandidateList(HIMC himc, uint listIndex, void* candidateList, uint bufferLength);
}
