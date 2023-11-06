using System;

namespace SkiaSharp;

internal static class SKTextBlobBuilderExtensions
{
	// This mostly mimics what SkiaSharp does.
	// Except that we avoid allocations of unnecessary SKPositionedRunBuffer
	public static unsafe UnoSKRunBufferInternal AllocatePositionedRunFast(this SKTextBlobBuilder builder, SKFont font, int count, SKRect? bounds = null)
	{
		if (font == null)
		{
			throw new ArgumentNullException(nameof(font));
		}

		UnoSKRunBufferInternal buffer = default(UnoSKRunBufferInternal);
		if (bounds.HasValue)
		{
			SKRect valueOrDefault = bounds.Value;
			UnoSkiaApi.sk_textblob_builder_alloc_run_pos(builder.Handle, font.Handle, count, &valueOrDefault, &buffer);
		}
		else
		{
			UnoSkiaApi.sk_textblob_builder_alloc_run_pos(builder.Handle, font.Handle, count, null, &buffer);
		}

		return buffer;
	}
}
