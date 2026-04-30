#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

/// <summary>
/// A half-open <c>[Start, End)</c> codepoint range together with the font families known to cover it.
/// Used as the input shape for <see cref="CoverageTableFontFallbackService"/>.
/// </summary>
public sealed record FontFallbackCoverageRange(int Start, int End, IReadOnlyList<string> Families);

/// <summary>
/// A reusable <see cref="IFontFallbackService"/> implementation that resolves fallback fonts
/// from a precomputed codepoint coverage table and an arbitrary font-stream provider.
/// </summary>
/// <remarks>
/// Construct this with your own <see cref="FontFallbackCoverageRange"/> table and stream-loading
/// strategy (embedded resource, private CDN, OS lookup, etc.), or copy the data shipped with the
/// default service to keep the same coverage while changing how font bytes are obtained - for
/// example, to avoid CORS-restricted hosts on WebAssembly.
/// </remarks>
public sealed class CoverageTableFontFallbackService : IFontFallbackService
{
	private readonly IReadOnlyList<FontFallbackCoverageRange> _coverageTable;
	private readonly Func<string, FontWeight, FontStretch, FontStyle, CancellationToken, Task<Stream?>> _fontStreamProvider;
	private readonly HashSet<int> _missingCodepoints = new();
	private readonly Dictionary<string, byte[]?> _fetchedFonts = new();
	private readonly Func<int, Task<string?>> _memoizedGetFontFamilyForCodepoint;
	private Task? _fetchTask;

	/// <summary>
	/// Creates a new fallback service backed by the given coverage table and font-stream provider.
	/// </summary>
	/// <param name="coverageTable">
	/// Codepoint ranges (half-open) and the family identifiers that cover them. The list is expected
	/// to be sorted by <see cref="FontFallbackCoverageRange.Start"/> and non-overlapping.
	/// </param>
	/// <param name="fontStreamProvider">
	/// Callback invoked once per family that the service decides to load. Returns a readable stream
	/// of font bytes which is fully read and cached, or <c>null</c> if no source is available
	/// for the family, in which case the family is recorded as unsupported.
	/// </param>
	public CoverageTableFontFallbackService(
		IReadOnlyList<FontFallbackCoverageRange> coverageTable,
		Func<string, FontWeight, FontStretch, FontStyle, CancellationToken, Task<Stream?>> fontStreamProvider)
	{
		_coverageTable = coverageTable ?? throw new ArgumentNullException(nameof(coverageTable));
		_fontStreamProvider = fontStreamProvider ?? throw new ArgumentNullException(nameof(fontStreamProvider));
		_memoizedGetFontFamilyForCodepoint = ((Func<int, Task<string?>>)GetFontFamilyForCodepointInternal).AsMemoized();
	}

	public Task<string?> GetFontFamilyForCodepoint(int codepoint) => _memoizedGetFontFamilyForCodepoint(codepoint);

	public async Task<Stream?> GetFontStreamForFontFamily(string fontFamily, FontWeight weight, FontStretch stretch, FontStyle style)
	{
		NativeDispatcher.CheckThreadAccess();
		if (_fetchedFonts.TryGetValue(fontFamily, out var bytes))
		{
			return bytes is null ? null : new MemoryStream(bytes, writable: false);
		}

		if (_fetchTask is null)
		{
			return null;
		}

		await _fetchTask;
		return _fetchedFonts.TryGetValue(fontFamily, out bytes) && bytes is not null
			? new MemoryStream(bytes, writable: false)
			: null;
	}

	private async Task<string?> GetFontFamilyForCodepointInternal(int codepoint)
	{
		NativeDispatcher.CheckThreadAccess();

		var family = FindFetchedFamilyForCodepoint(codepoint);
		if (family is not null)
		{
			return family;
		}

		_missingCodepoints.Add(codepoint);
		if (_fetchTask is null)
		{
			var fetchTask = FetchFontsForMissingCodepoints();
			if (!fetchTask.IsCompleted)
			{
				_fetchTask = fetchTask;
				await fetchTask;
			}
		}
		else
		{
			await _fetchTask;
		}

		return FindFetchedFamilyForCodepoint(codepoint);
	}

	private string? FindFetchedFamilyForCodepoint(int codepoint)
	{
		foreach (var family in GetFamiliesForCodepoint(codepoint, _coverageTable))
		{
			if (_fetchedFonts.TryGetValue(family, out var bytes) && bytes is not null)
			{
				return family;
			}
		}

		return null;
	}

	private async Task FetchFontsForMissingCodepoints()
	{
		NativeDispatcher.CheckThreadAccess();

		var missingCodepoints = _missingCodepoints.ToList();
		_missingCodepoints.Clear();

		var families = SelectMinimalCoveringFamilies(missingCodepoints, _coverageTable);
		foreach (var family in families)
		{
			Stream? stream = null;
			Exception? streamError = null;
			try
			{
				// TODO: use weight/stretch/style to pick the best match. Eager fetch currently
				// happens at codepoint-resolution time, before any specific style is known.
				stream = await _fontStreamProvider(family, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal, CancellationToken.None);
			}
			catch (Exception e)
			{
				streamError = e;
			}

			byte[]? bytes = null;
			Exception? readError = null;
			if (streamError is null && stream is not null)
			{
				try
				{
					bytes = await ReadAllBytesAsync(stream);
				}
				catch (Exception e)
				{
					readError = e;
				}
				finally
				{
					stream.Dispose();
				}
			}

			// The customer-supplied provider may resume on any context. Re-dispatch state
			// mutations to the UI thread so _fetchedFonts (and other UI-thread state) stays coherent.
			await NativeDispatcher.Main.EnqueueAsync(() =>
			{
				if (streamError is not null)
				{
					this.LogError()?.Error($"Font stream provider failed for {family}", streamError);
					_fetchedFonts[family] = null;
					return;
				}

				if (readError is not null)
				{
					this.LogError()?.Error($"Failed to read font bytes for {family}", readError);
					_fetchedFonts[family] = null;
					return;
				}

				_fetchedFonts[family] = bytes;
			});
		}

		if (_missingCodepoints.Count > 0)
		{
			await FetchFontsForMissingCodepoints();
		}
		else
		{
			_fetchTask = null;
		}
	}

	private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
	{
		if (stream is MemoryStream ms && ms.TryGetBuffer(out var seg) && seg.Offset == 0 && seg.Count == seg.Array!.Length)
		{
			return seg.Array;
		}

		using var copy = new MemoryStream();
		await stream.CopyToAsync(copy);
		return copy.ToArray();
	}

	private static List<string> SelectMinimalCoveringFamilies(IEnumerable<int> codepoints, IReadOnlyList<FontFallbackCoverageRange> coverageTable)
	{
		var requested = codepoints.Distinct().ToList();
		requested.Sort();
		if (requested.Count == 0)
		{
			return [];
		}

		var uncovered = new HashSet<int>(requested);
		var coverageByFamily = BuildCoverageMap(requested, coverageTable);
		var selected = new List<string>();

		while (uncovered.Count > 0)
		{
			var (bestFamily, coveredCount) = FindBestCoveringFamily(uncovered, coverageByFamily);
			if (bestFamily is null || coveredCount == 0)
			{
				break;
			}

			selected.Add(bestFamily);
			foreach (var cp in coverageByFamily[bestFamily])
			{
				uncovered.Remove(cp);
			}

			coverageByFamily.Remove(bestFamily);
		}

		return selected;
	}

	private static Dictionary<string, HashSet<int>> BuildCoverageMap(IEnumerable<int> requested, IReadOnlyList<FontFallbackCoverageRange> coverageTable)
	{
		var map = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
		foreach (var codepoint in requested)
		{
			var families = GetFamiliesForCodepoint(codepoint, coverageTable);
			foreach (var family in families)
			{
				if (!map.TryGetValue(family, out var covered))
				{
					covered = new HashSet<int>();
					map[family] = covered;
				}

				covered.Add(codepoint);
			}
		}

		return map;
	}

	private static IReadOnlyList<string> GetFamiliesForCodepoint(int codepoint, IReadOnlyList<FontFallbackCoverageRange> coverageTable)
	{
		for (int i = 0; i < coverageTable.Count; i++)
		{
			var range = coverageTable[i];
			if (codepoint < range.Start)
			{
				break;
			}

			if (codepoint >= range.Start && codepoint < range.End)
			{
				return range.Families;
			}
		}

		return Array.Empty<string>();
	}

	private static (string? Family, int CoveredCount) FindBestCoveringFamily(HashSet<int> uncovered, Dictionary<string, HashSet<int>> coverageByFamily)
	{
		string? bestFamily = null;
		int bestCoverage = 0;

		foreach (var (family, covered) in coverageByFamily)
		{
			int current = 0;
			foreach (var cp in covered)
			{
				if (uncovered.Contains(cp))
				{
					current++;
				}
			}

			if (current > bestCoverage)
			{
				bestCoverage = current;
				bestFamily = family;
			}
		}

		return (bestFamily, bestCoverage);
	}
}
