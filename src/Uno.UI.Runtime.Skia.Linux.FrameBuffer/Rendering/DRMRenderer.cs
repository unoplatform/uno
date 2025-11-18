using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Foundation;
using SkiaSharp;
using Uno.UI.Runtime.Skia.Native;
using Uno.Foundation.Logging;
using System.Text.RegularExpressions;
using System.Threading;
using Windows.Graphics.Display;
using Windows.Graphics.Interop.Direct2D;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers;
using Uno.UI.Hosting;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace Uno.UI.Runtime.Skia
{
	partial class DRMRenderer : FrameBufferRenderer
	{
		private const uint DefaultFramebuffer = 0;

		private readonly GRContext _grContext;
		private readonly IntPtr _eglDisplay;
		private readonly IntPtr _glContext;
		private readonly IntPtr _eglSurface;
		private readonly int _samples;
		private readonly int _stencil;

		private GRBackendRenderTarget? _renderTarget;
		private readonly IntPtr _gbmTargetSurface;
		private readonly int _card;
		private IntPtr _currentBo;
		private readonly uint _crtc;
		private readonly uint _encoder;
		private bool _waitingForPageFlip;
		private bool _invalidateRenderCalledWhileWaitingForPageFlip;

		public unsafe DRMRenderer(IXamlRootHost host) : base(host)
		{
			var cardPath = FeatureConfiguration.LinuxFramebuffer.DRMCardPath;
			if (cardPath is not null)
			{
				_card = Libc.open(cardPath, Libc.O_RDWR, 0);
				if (_card == -1)
				{
					var errno = Marshal.GetLastWin32Error();
					var errnoStringPtr = Libc.strerror(errno);
					var errorString = Marshal.PtrToStringAnsi(errnoStringPtr);
					throw new InvalidOperationException($"Couldn't open {cardPath} ({errno}): {errorString}");
				}
				else
				{
					this.LogInfo()?.Info($"Found DRM device {cardPath}");
				}
			}
			else
			{
				var files = Directory.GetFiles("/dev/dri/");

				foreach (var file in files)
				{
					if (DRMCardPathRegex().Match(file).Success)
					{
						_card = Libc.open(file, Libc.O_RDWR, 0);
						if (_card == -1)
						{
							var errno = Marshal.GetLastWin32Error();
							var errnoStringPtr = Libc.strerror(errno);
							var errorString = Marshal.PtrToStringAnsi(errnoStringPtr);
							this.LogDebug()?.LogDebug($"Couldn't open {file} ({errno}): {errorString}");
						}
						else
						{
							this.LogInfo()?.Info($"Found DRM device {file}");
							break;
						}
					}
				}
				if (_card == -1)
				{
					throw new FileNotFoundException("Couldn't open any DRM card matching /dev/dri/card[0-9]+");
				}
			}

			var resources = new DrmResources(_card);
			this.LogDebug()?.Debug($"DRM resources dump:\n{resources.Dump()}");

			if (resources.Connectors.Count == 0)
			{
				throw new Exception("No DRM connectors found");
			}

			var connectors =
				resources.Connectors
				.Where(c => c is { Connection: DrmModeConnection.DRM_MODE_CONNECTED, Modes.Count: > 0 })
				.ToList();
			DrmConnector? connector = default;
			if (FeatureConfiguration.LinuxFramebuffer.DRMConnectorChooser is { } chooser)
			{
				var connectorsForChooser =
					connectors
						.Select(c => new FeatureConfiguration.LinuxFramebuffer.DRMConnector((uint)c.ConnectorType, c.ConnectorTypeId, c.Id, c.Name))
						.ToList();
				if (chooser(connectorsForChooser) is var chosenConnectorIndex && connectorsForChooser.Count > chosenConnectorIndex && chosenConnectorIndex >= 0)
				{
					connector = connectors[chosenConnectorIndex];
				}
				else
				{
					throw new InvalidOperationException($"The connector chosen with {nameof(FeatureConfiguration.LinuxFramebuffer.DRMConnectorChooser)} does not have a usable CRTC+encoder combination");
				}
			}
			else
			{
				// We use the first connector that has a usable encoder+crtc combination
				foreach (var connectorCandidate in connectors)
				{
					var encoderIds = resources.Encoders.Keys.AsEnumerable();
					if (resources.Encoders.ContainsKey(connectorCandidate.EncoderId))
					{
						// if connector is already modeset to use a specific encoder, then let's try reusing it first
						encoderIds = encoderIds.Prepend(connectorCandidate.EncoderId);
					}
					foreach (var encoderId in encoderIds)
					{
						var encoder = resources.Encoders[encoderId];
						if (encoder.PossibleCrtcs.Any(crtc => crtc.crtc_id == encoder.Encoder.crtc_id))
						{
							connector = connectorCandidate;
							_encoder = encoderId;
							_crtc = encoder.Encoder.crtc_id;
							break;
						}
						else if (encoder.PossibleCrtcs.Count > 0)
						{
							connector = connectorCandidate;
							_encoder = encoderId;
							// possible crtcs are ordered from best to worst
							_crtc = encoder.PossibleCrtcs.First().crtc_id;
							break;
						}
					}
				}

				if (connector is null)
				{
					throw new InvalidOperationException("Cannot find any connectors with a usable CRTC+encoder combination");
				}
			}

			Debug.Assert(connector is not null && resources.Encoders[_encoder].PossibleCrtcs.Any(crtc => _crtc == crtc.crtc_id));

			var modeInfo = connector.Modes.FirstOrDefault(m => m.IsPreferred, connector.Modes[0]);

			var device = LibDrm.gbm_create_device(_card);
			if (device == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(LibDrm.gbm_create_device)} failed");
			}
			_gbmTargetSurface = LibDrm.gbm_surface_create(device, modeInfo.Resolution.Width, modeInfo.Resolution.Height,
				LibDrm.GbmColorFormats.GBM_FORMAT_XRGB8888, LibDrm.GbmBoFlags.GBM_BO_USE_SCANOUT | LibDrm.GbmBoFlags.GBM_BO_USE_RENDERING);
			if (_gbmTargetSurface == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(LibDrm.gbm_surface_create)} failed");
			}

			try
			{
				_eglDisplay = EglHelper.EglGetPlatformDisplay(/* EGL_PLATFORM_GBM_KHR */ 0x31D7, device, null);
				if (_eglDisplay == IntPtr.Zero)
				{
					throw new InvalidOperationException($"{nameof(EglHelper.EglGetPlatformDisplay)} failed : {Enum.GetName(EglHelper.EglGetError())}");
				}
			}
			catch (Exception e)
			{
				this.LogDebug()?.Debug(e.Message);
				_eglDisplay = EglHelper.EglGetPlatformDisplayEXT(/* EGL_PLATFORM_GBM_KHR */ 0x31D7, device, null);
				if (_eglDisplay == IntPtr.Zero)
				{
					throw new InvalidOperationException($"{nameof(EglHelper.EglGetPlatformDisplayEXT)} failed : {Enum.GetName(EglHelper.EglGetError())}");
				}
			}

			EglHelper.EglInitialize(_eglDisplay, out var major, out var minor);
			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info($"Found EGL version {major}.{minor}.");
			}

			int[] attribList =
			{
				EglHelper.EGL_RED_SIZE, 8,
				EglHelper.EGL_GREEN_SIZE, 8,
				EglHelper.EGL_BLUE_SIZE, 8,
				EglHelper.EGL_ALPHA_SIZE, 8,
				EglHelper.EGL_DEPTH_SIZE, 8,
				EglHelper.EGL_STENCIL_SIZE, 1,
				EglHelper.EGL_RENDERABLE_TYPE, EglHelper.EGL_OPENGL_ES2_BIT,
				EglHelper.EGL_NONE
			};

			var configs = new IntPtr[1];
			var success = EglHelper.EglChooseConfig(_eglDisplay, attribList, configs, configs.Length, out var numConfig);

			if (!success || numConfig < 1)
			{
				throw new InvalidOperationException($"{nameof(EglHelper.EglChooseConfig)} failed: {Enum.GetName(EglHelper.EglGetError())}");
			}

			if (!EglHelper.EglGetConfigAttrib(_eglDisplay, configs[0], EglHelper.EGL_SAMPLES, out _samples))
			{
				throw new InvalidOperationException($"{nameof(EglHelper.EglGetConfigAttrib)} failed to get {nameof(EglHelper.EGL_SAMPLES)}: {Enum.GetName(EglHelper.EglGetError())}");
			}
			if (!EglHelper.EglGetConfigAttrib(_eglDisplay, configs[0], EglHelper.EGL_STENCIL_SIZE, out _stencil))
			{
				throw new InvalidOperationException($"{nameof(EglHelper.EglGetConfigAttrib)} failed to get {nameof(EglHelper.EGL_STENCIL_SIZE)}: {Enum.GetName(EglHelper.EglGetError())}");
			}

			_glContext = EglHelper.EglCreateContext(_eglDisplay, configs[0], EglHelper.EGL_NO_CONTEXT, [EglHelper.EGL_CONTEXT_CLIENT_VERSION, 2, EglHelper.EGL_NONE]);
			if (_glContext == IntPtr.Zero)
			{
				throw new InvalidOperationException($"EGL context creation failed: {Enum.GetName(EglHelper.EglGetError())}");
			}

			_eglSurface = EglHelper.EglCreatePlatformWindowSurface(_eglDisplay, configs[0], _gbmTargetSurface, [EglHelper.EGL_NONE]);

			using var _ = MakeCurrent();

			if (!EglHelper.EglSwapBuffers(_eglDisplay, _eglSurface))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(EglHelper.EglSwapBuffers)} failed during Renderer init: {Enum.GetName(EglHelper.EglGetError())}");
				}
			}

			var bo = LibDrm.gbm_surface_lock_front_buffer(_gbmTargetSurface);
			if (bo == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(LibDrm.gbm_surface_lock_front_buffer)} failed during DRM CRTC setup.");
			}
			var fbId = CreateFbForBo(bo);
			var connectorId = connector.Id;
			var mode = modeInfo.Mode;
			
			var res = LibDrm.drmModeSetCrtc(_card, _crtc, fbId, 0, 0, &connectorId, 1, &mode);
			if (res != 0)
			{
				throw new InvalidOperationException($"{nameof(LibDrm.drmModeSetCrtc)} failed with error code {res}");
			}

			_currentBo = bo;

			var glInterface = GRGlInterface.CreateGles(EglHelper.EglGetProcAddress);

			if (glInterface == null)
			{
				throw new NotSupportedException($"{nameof(GRGlInterface)}.{nameof(GRGlInterface.CreateGles)} failed");
			}

			var context = GRContext.CreateGl(glInterface);
			if (context == null)
			{
				throw new NotSupportedException($"{nameof(GRContext)}.{nameof(GRContext.CreateGl)} failed");
			}
			_grContext = context;

			var glGetString = (delegate* unmanaged[Cdecl]<int, byte*>)EglHelper.EglGetProcAddress("glGetString");

			var glVersionBytePtr = glGetString(/* GL_VERSION */ 0x1F02);
			var glVersionString = Marshal.PtrToStringUTF8((IntPtr)glVersionBytePtr);

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info($"Using {glVersionString} for rendering.");
			}

			FrameBufferWindowWrapper.Instance.SetSize(new Size(modeInfo.Resolution.Width, modeInfo.Resolution.Height), (float)DisplayInformation.GetForCurrentViewSafe().RawPixelsPerViewPixel);

			new Thread(PageFlipLoop) { IsBackground = true, Name = "DRM pageflip loop" }.Start();
		}

		private unsafe int CalculateRefreshRate(LibDrm.drmModeModeInfo *mode)
		{
			var res = (int)(mode->clock * 1000000L / mode->htotal + mode->vtotal / 2) / mode->vtotal;

			if ((mode->flags & /* DRM_MODE_FLAG_INTERLACE */ (1<<4)) != 0)
			{
				res *= 2;
			}

			if ((mode->flags & /* DRM_MODE_FLAG_DBLSCAN */ (1<<5)) != 0)
			{
				res /= 2;
			}

			if (mode->vscan > 1)
			{
				res /= mode->vscan;
			}

			return res / 1000;
		}

		public override unsafe void InvalidateRender()
		{
			Volatile.Write(ref _invalidateRenderCalledWhileWaitingForPageFlip, true);
			if (Interlocked.Exchange(ref _waitingForPageFlip, true))
			{
				return;
			}

			using (MakeCurrent())
			{
				if (!EglHelper.EglSwapBuffers(_eglDisplay, _eglSurface))
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"{nameof(EglHelper.EglSwapBuffers)} failed.");
					}
				}
			}
			var nextBo = LibDrm.gbm_surface_lock_front_buffer(_gbmTargetSurface);
			if (nextBo == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(LibDrm.gbm_surface_lock_front_buffer)} failed");
			}

			LibDrm.gbm_surface_release_buffer(_gbmTargetSurface, _currentBo);
			_currentBo = nextBo;

			var fb = CreateFbForBo(nextBo);
			var res = LibDrm.drmModePageFlip(_card, _crtc, fb, LibDrm.DrmModePageFlip.Event, null);
			if (res != 0)
			{
				throw new InvalidOperationException($"{nameof(LibDrm.drmModePageFlip)} failed ({res})");
			}
		}

		protected override IDisposable MakeCurrent()
		{
			var glContext = EglHelper.EglGetCurrentContext();
			var readSurface = EglHelper.EglGetCurrentSurface(EglHelper.EGL_READ);
			var drawSurface = EglHelper.EglGetCurrentSurface(EglHelper.EGL_DRAW);
			if (!EglHelper.EglMakeCurrent(_eglDisplay, _eglSurface, _eglSurface, _glContext))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(EglHelper.EglMakeCurrent)} failed.");
				}
			}
			return Disposable.Create(() =>
			{
				if (!EglHelper.EglMakeCurrent(_eglDisplay, drawSurface, readSurface, glContext))
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"{nameof(EglHelper.EglMakeCurrent)} failed.");
					}
				}
			});
		}

		private unsafe void PageFlipLoop()
		{
			var ctx = new LibDrm.DrmEventContext
			{
				version = 4,
				page_flip_handler2 = Marshal.GetFunctionPointerForDelegate(OnPageFlip)
			};
			while (true)
			{
				var pfd = new pollfd {events = 1, fd = _card};
				var res = Libc.poll(&pfd, new IntPtr(1), -1);
				if (res < 0)
				{
					var errno = Marshal.GetLastWin32Error();
					var errnoStringPtr = Libc.strerror(errno);
					var errorString = Marshal.PtrToStringAnsi(errnoStringPtr);
					throw new InvalidOperationException($"{nameof(Libc.poll)} failed ({errno}) : {errorString}");
				}

				res = LibDrm.drmHandleEvent(_card, &ctx);
				if (res != 0)
				{
					throw new InvalidOperationException($"{nameof(LibDrm.drmHandleEvent)} failed ({res})");
				}
			}
		}

		private unsafe void OnPageFlip(int fd, uint sequence, uint tv_sec, uint tv_usec, void* user_data)
		{
			Volatile.Write(ref _invalidateRenderCalledWhileWaitingForPageFlip, false);
			Render();
			Volatile.Write(ref _waitingForPageFlip, false);
			if (Volatile.Read(ref _invalidateRenderCalledWhileWaitingForPageFlip))
			{
				InvalidateRender();
			}
		}

		protected override SKSurface UpdateSize(int width, int height)
		{
			_renderTarget?.Dispose();

			var grSurfaceOrigin = GRSurfaceOrigin.BottomLeft; // to match OpenGL's origin

			var glInfo = new GRGlFramebufferInfo(DefaultFramebuffer, SKColorType.Rgb888x.ToGlSizedFormat());

			_renderTarget = new GRBackendRenderTarget(width, height, _samples, _stencil, glInfo);
			return SKSurface.Create(_grContext, _renderTarget, grSurfaceOrigin, SKColorType.Rgb888x);
		}

		private uint CreateFbForBo(IntPtr bo)
		{
			if (bo == IntPtr.Zero)
				throw new ArgumentException("bo is 0");
			var data = LibDrm.gbm_bo_get_user_data(bo);
			if (data != IntPtr.Zero)
				return (uint)data.ToInt32();

			var w = LibDrm.gbm_bo_get_width(bo);
			var h = LibDrm.gbm_bo_get_height(bo);
			var stride = LibDrm.gbm_bo_get_stride(bo);
			var handle = LibDrm.gbm_bo_get_handle(bo).u32;
			var format = LibDrm.gbm_bo_get_format(bo);

			// prepare for the new ioctl call
			var handles = new uint[] { handle, 0, 0, 0 };
			var pitches = new uint[] { stride, 0, 0, 0 };
			var offsets = new uint[4];

			var ret = LibDrm.drmModeAddFB2(_card, w, h, format, handles, pitches,
				offsets, out var fbHandle, 0);
			if (ret != 0)
			{
				throw new InvalidOperationException($"{nameof(LibDrm.drmModeAddFB2)} failed {ret}");
			}

			LibDrm.gbm_bo_set_user_data(bo, new IntPtr((int)fbHandle), OnBoFree);

			return fbHandle;
		}

		private void OnBoFree(IntPtr bo, IntPtr fbHandle) => LibDrm.drmModeRmFB(_card, fbHandle.ToInt32());

		[GeneratedRegex("card[0-9]+")]
		private static partial Regex DRMCardPathRegex();
	}
}
