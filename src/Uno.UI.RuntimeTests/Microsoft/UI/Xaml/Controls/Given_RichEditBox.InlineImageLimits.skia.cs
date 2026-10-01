#nullable enable

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	// 2048 x 2048 is exactly the decoded-pixel budget for a single frame.
	private const int BudgetEdge = 2048;

	[TestMethod]
	public void When_Gif_Single_Frame_Fits_Pixel_Budget_Image_Is_Accepted()
		=> Assert.IsTrue(TryCreateInlineImage(CreateGif(BudgetEdge, BudgetEdge, frameCount: 1)));

	[TestMethod]
	public void When_Animated_Gif_Frames_Exceed_Pixel_Budget_Image_Is_Rejected()
		=> Assert.IsFalse(TryCreateInlineImage(CreateGif(BudgetEdge, BudgetEdge, frameCount: 2)));

	[TestMethod]
	public void When_Animated_Png_Frames_Exceed_Pixel_Budget_Image_Is_Rejected()
		=> Assert.IsFalse(TryCreateInlineImage(CreateApngHeader(BudgetEdge, BudgetEdge, frameCount: 2)));

	[TestMethod]
	public void When_Bmp_Height_Is_Int_MinValue_Image_Is_Rejected()
	{
		var bmp = new byte[54];
		bmp[0] = (byte)'B';
		bmp[1] = (byte)'M';
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(2), bmp.Length);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14), 40);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18), 16);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22), int.MinValue);

		Assert.IsFalse(TryCreateInlineImage(bmp));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Idle_Caret_Stops_Blinking_After_Timeout()
	{
		var editor = new RichEditBox { Width = 200 };
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();

			// WinUI stops blinking after TextSelectionSettings' 5 s caret blink timeout and leaves the caret shown.
			await Task.Delay(TimeSpan.FromSeconds(6));
			for (var i = 0; i < 4; i++)
			{
				Assert.IsTrue(editor.IsCaretRenderedForTesting, $"The caret should stay visible once blinking stops (sample {i}).");
				await Task.Delay(300);
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static bool TryCreateInlineImage(byte[] data)
		=> InlineImageState.TryCreate(
			data,
			width: null,
			height: null,
			ascent: null,
			VerticalCharacterAlignment.Baseline,
			alternateText: null,
			InlineImageEncoding.Unknown,
			out _);

	private static byte[] CreateGif(int width, int height, int frameCount)
	{
		var gif = new List<byte>();
		gif.AddRange("GIF89a"u8.ToArray());
		gif.AddRange(BitConverter.GetBytes((ushort)width));
		gif.AddRange(BitConverter.GetBytes((ushort)height));
		gif.AddRange(new byte[] { 0x80, 0, 0 }); // 2-entry global color table
		gif.AddRange(new byte[] { 0, 0, 0, 255, 255, 255 });
		for (var frame = 0; frame < frameCount; frame++)
		{
			// A 1x1 sub-frame: tiny on disk, but the decoder still materializes a full canvas per frame.
			gif.AddRange(new byte[] { 0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0 });
			gif.AddRange(new byte[] { 0x02, 0x02, 0x44, 0x01, 0x00 });
		}
		gif.Add(0x3B);
		return gif.ToArray();
	}

	private static byte[] CreateApngHeader(int width, int height, int frameCount)
	{
		var png = new List<byte>();
		png.AddRange(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
		var ihdr = new byte[13];
		BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(0), width);
		BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), height);
		ihdr[8] = 8;
		ihdr[9] = 6;
		AddChunk("IHDR", ihdr);
		var actl = new byte[8];
		BinaryPrimitives.WriteInt32BigEndian(actl.AsSpan(0), frameCount);
		AddChunk("acTL", actl);
		AddChunk("IEND", []);
		return png.ToArray();

		void AddChunk(string type, byte[] data)
		{
			var length = new byte[4];
			BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
			png.AddRange(length);
			png.AddRange(System.Text.Encoding.ASCII.GetBytes(type));
			png.AddRange(data);
			png.AddRange(new byte[4]); // CRC is not validated by the size probe
		}
	}
}
