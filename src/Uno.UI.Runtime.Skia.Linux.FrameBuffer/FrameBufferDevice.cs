// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Base interactions with the Linux framebuffer derived from https://github.com/AvaloniaUI/Avalonia

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using SkiaSharp;
using Uno.UI.Runtime.Skia.Native;
using Windows.Foundation;

namespace Uno.UI.Runtime.Skia
{
	/// <summary>
	/// Linux FrameBuffer device support
	/// </summary>
	unsafe class FrameBufferDevice
	{
		private int _fd;
		private fb_fix_screeninfo _fixedInfo;
		private fb_var_screeninfo _screenInfo;
		private IntPtr _mappedLength;
		private IntPtr _mappedAddress;

		/// <summary>
		/// Size of the screen in pixels
		/// </summary>
		public Size ScreenSize { get; private set; }

		/// <summary>
		/// Number of bytes for an individual row
		/// </summary>
		public int RowBytes => (int)_fixedInfo.line_length;

		/// <summary>
		/// Pixel format information
		/// </summary>
		public SKColorType PixelFormat
			=> _screenInfo.blue.offset == 16 ? SKColorType.Rgba8888 : SKColorType.Bgra8888;

		/// <summary>
		/// Builds a FrameBufferDevice instance.
		/// </summary>
		/// <param name="fileName">The name of the FrameBuffer device, otherwise uses the FRAMEBUFFER environment variable</param>
		public FrameBufferDevice(string? fileName = null)
		{
			fileName ??=
				Environment.GetEnvironmentVariable("FRAMEBUFFER")
				?? "/dev/fb0";

			_fd = Libc.open(fileName, Libc.O_RDWR, 0);
			if (_fd <= 0)
			{
				throw new InvalidOperationException($"Failed to open FrameBuffer device {fileName} ({Marshal.GetLastWin32Error()})");
			}
		}

		public void Init()
		{
			fixed (void* pScreenInfo = &_screenInfo)
			{
				if (Libc.ioctl(_fd, FbIoCtl.FBIOGET_VSCREENINFO, pScreenInfo) == -1)
				{
					throw new InvalidOperationException($"Failed to invoke FBIOGET_VSCREENINFO ({Marshal.GetLastWin32Error()}");
				}

				Set32BitsPixelFormat();

				if (Libc.ioctl(_fd, FbIoCtl.FBIOPUT_VSCREENINFO, pScreenInfo) == -1)
				{
					_screenInfo.transp = new fb_bitfield();
				}

				Libc.ioctl(_fd, FbIoCtl.FBIOPUT_VSCREENINFO, pScreenInfo);

				if (Libc.ioctl(_fd, FbIoCtl.FBIOGET_VSCREENINFO, pScreenInfo) == -1)
				{
					throw new InvalidOperationException($"Failed to invoke FBIOGET_VSCREENINFO ({Marshal.GetLastWin32Error()}");
				}

				if (_screenInfo.bits_per_pixel != 32)
				{
					throw new InvalidOperationException("Failed to set 32 bits display mode");
				}
			}

			ScreenSize = new Size((int)_screenInfo.xres, (int)_screenInfo.yres);

			fixed (void* pFixedInfo = &_fixedInfo)
			{
				if (Libc.ioctl(_fd, FbIoCtl.FBIOGET_FSCREENINFO, pFixedInfo) == -1)
				{
					throw new InvalidOperationException($"Failed to invoke FBIOGET_FSCREENINFO ({Marshal.GetLastWin32Error()})");
				}
			}

			_mappedLength = new IntPtr(_fixedInfo.line_length * _screenInfo.yres);
			_mappedAddress = Libc.mmap(
				IntPtr.Zero,
				_mappedLength,
				Libc.PROT_READ | Libc.PROT_WRITE,
				Libc.MAP_SHARED,
				_fd,
				IntPtr.Zero);

			if (_mappedAddress == new IntPtr(-1))
			{
				throw new InvalidOperationException($"Failed to map {_mappedLength} bytes ({Marshal.GetLastWin32Error()})");
			}
		}

		void Set32BitsPixelFormat()
		{
			_screenInfo.bits_per_pixel = 32;
			_screenInfo.grayscale = 0;
			_screenInfo.red = new fb_bitfield { length = 8 };
			_screenInfo.blue = new fb_bitfield { length = 8 };
			_screenInfo.green = new fb_bitfield { length = 8 };
			_screenInfo.transp = new fb_bitfield { length = 8 };
			_screenInfo.green.offset = 8;
			_screenInfo.blue.offset = 16;
			_screenInfo.transp.offset = 24;
		}

		public IntPtr BufferAddress
		{
			get
			{
				if (_fd <= 0)
				{
					throw new ObjectDisposedException("FrameBufferDevice");
				}

				return _mappedAddress;
			}
		}

		private void Dispose(bool disposing)
		{
			if (_mappedAddress != IntPtr.Zero)
			{
				Libc.munmap(_mappedAddress, _mappedLength);
				_mappedAddress = IntPtr.Zero;
			}

			if (_fd == 0)
			{
				return;
			}

			Libc.close(_fd);
			_fd = 0;
		}

		public void VSync()
		{
			if (_fd <= 0)
			{
				throw new ObjectDisposedException("FrameBufferDevice");
			}

			Libc.ioctl(_fd, FbIoCtl.FBIO_WAITFORVSYNC, null);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~FrameBufferDevice()
		{
			Dispose(false);
		}
	}
}
