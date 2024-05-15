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
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Native;
using Windows.ApplicationModel.Activation;
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
		public SKColorType PixelFormat =>
			_screenInfo.bits_per_pixel switch
			{
				// 32 bits
				//
				// Sample: (RPi 4b 64bits w/ disabled 3d acceleration)
				//		red
				//		  red.offset = 16
				//		  red.length = 8
				//		  red.msb_right = 0
				//		green
				//		  green.offset = 8
				//		  green.length = 8
				//		  green.msb_right = 0
				//		blue
				//		  blue.offset = 0
				//		  blue.length = 8
				//		  blue.msb_right = 0
				//		transp
				//		  transp.offset = 24
				//		  transp.length = 8
				//		  transp.msb_right = 0
				32 => _screenInfo.blue.offset switch
				{
					0 => SKColorType.Bgra8888,
					16 => SKColorType.Rgba8888,
					_ => throw new NotSupportedException($"Framebuffer configuration with blue offset of {_screenInfo.blue.offset} is not yet supported")
				},

				// 16 bits
				//
				//  Sample: (RPi 4b 64bits w/ 3d acceleration)
				//  red
				//	  red.offset = 11
				//	  red.length = 5
				//	  red.msb_right = 0
				//	green
				//	  green.offset = 5
				//	  green.length = 6
				//	  green.msb_right = 0
				//	blue
				//	  blue.offset = 0
				//	  blue.length = 5
				//	  blue.msb_right = 0
				//	transp
				//	  transp.offset = 0
				//	  transp.length = 0
				//	  transp.msb_right = 0

				16 => _screenInfo.red.offset == 11
					? SKColorType.Rgb565
					: throw new NotSupportedException($"RGB555 is not supported by Uno Platform"),

				_ => throw new NotSupportedException($"{_screenInfo.bits_per_pixel} bpp framebuffer is not supported"),
			};

		/// <summary>
		/// Screen physical size in millimeters
		/// </summary>
		public Size ScreenPhysicalDimensions
			=> new((int)_screenInfo.width, (int)_screenInfo.height);

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

#pragma warning disable CA1806 // Do not ignore method results
				Libc.ioctl(_fd, FbIoCtl.FBIOPUT_VSCREENINFO, pScreenInfo);
#pragma warning restore CA1806 // Do not ignore method results

				if (Libc.ioctl(_fd, FbIoCtl.FBIOGET_VSCREENINFO, pScreenInfo) == -1)
				{
					throw new InvalidOperationException($"Failed to invoke FBIOGET_VSCREENINFO ({Marshal.GetLastWin32Error()}");
				}

				if (_screenInfo.bits_per_pixel != 32)
				{
					if (_screenInfo.bits_per_pixel != 16)
					{
						throw new InvalidOperationException($"Failed to set 32 bits display mode (Found {_screenInfo.bits_per_pixel})");
					}
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

			LogFramebufferInformation();
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
#pragma warning disable CA1806 // Do not ignore method results
				Libc.munmap(_mappedAddress, _mappedLength);
#pragma warning restore CA1806 // Do not ignore method results
				_mappedAddress = IntPtr.Zero;
			}

			if (_fd == 0)
			{
				return;
			}

#pragma warning disable CA1806 // Do not ignore method results
			Libc.close(_fd);
#pragma warning restore CA1806 // Do not ignore method results
			_fd = 0;
		}

		public void VSync()
		{
			if (_fd <= 0)
			{
				throw new ObjectDisposedException("FrameBufferDevice");
			}

#pragma warning disable CA1806 // Do not ignore method results
			Libc.ioctl(_fd, FbIoCtl.FBIO_WAITFORVSYNC, null);
#pragma warning restore CA1806 // Do not ignore method results
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

		private void LogFramebufferInformation()
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace(
					$"""
					Framebuffer information:
						xres = {_screenInfo.xres}
						yres = {_screenInfo.yres}
						xres_virtual = {_screenInfo.xres_virtual}
						yres_virtual = {_screenInfo.yres_virtual}
						xoffset = {_screenInfo.xoffset}
						yoffset = {_screenInfo.yoffset}
						bits_per_pixel = {_screenInfo.bits_per_pixel}
						grayscale = {_screenInfo.grayscale}
						red
						  red.offset = {_screenInfo.red.offset}
						  red.length = {_screenInfo.red.length}
						  red.msb_right = {_screenInfo.red.msb_right}
						green
						  green.offset = {_screenInfo.green.offset}
						  green.length = {_screenInfo.green.length}
						  green.msb_right = {_screenInfo.green.msb_right}
						blue
						  blue.offset = {_screenInfo.blue.offset}
						  blue.length = {_screenInfo.blue.length}
						  blue.msb_right = {_screenInfo.blue.msb_right}
						transp
						  transp.offset = {_screenInfo.transp.offset}
						  transp.length = {_screenInfo.transp.length}
						  transp.msb_right = {_screenInfo.transp.msb_right}
						nonstd = {_screenInfo.nonstd}
						activate = {_screenInfo.activate}
						height = {_screenInfo.height}
						width = {_screenInfo.width}
						accel_flags = {_screenInfo.accel_flags}
						pixclock = {_screenInfo.pixclock}
						left_margin = {_screenInfo.left_margin}
						right_margin = {_screenInfo.right_margin}
						upper_margin = {_screenInfo.upper_margin}
						lower_margin = {_screenInfo.lower_margin}
						hsync_len = {_screenInfo.hsync_len}
						vsync_len = {_screenInfo.vsync_len}
						sync = {_screenInfo.sync}
						vmode = {_screenInfo.vmode}
					""");
			}
		}
	}
}
