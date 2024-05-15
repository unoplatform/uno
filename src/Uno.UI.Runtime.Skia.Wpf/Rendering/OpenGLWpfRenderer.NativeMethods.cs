#nullable enable

using System;
using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Skia.Wpf.Rendering;

internal partial class OpenGLWpfRenderer
{
	internal static class NativeMethods
	{
		[DllImport("user32.dll")]
		internal static extern IntPtr GetDC(IntPtr hWnd);

		[DllImport("user32.dll")]
		internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

		[DllImport("gdi32.dll")]
		internal static extern int ChoosePixelFormat(IntPtr hdc, ref PIXELFORMATDESCRIPTOR ppfd);

		[DllImport("gdi32.dll")]
		internal static extern int SetPixelFormat(IntPtr hdc, int iPixelFormat, ref PIXELFORMATDESCRIPTOR ppfd);

		[DllImport("opengl32.dll")]
		internal static extern IntPtr wglCreateContext(IntPtr hdc);

		[DllImport("opengl32.dll")]
		public static extern IntPtr wglGetCurrentDC();

		[DllImport("opengl32.dll")]
		internal static extern IntPtr wglCreateCompatibleDC(IntPtr hdc);

		[DllImport("opengl32.dll")]
		internal static extern int wglDeleteContext(IntPtr hglrc);

		[DllImport("opengl32.dll")]
		internal static extern int wglMakeCurrent(IntPtr hdc, IntPtr hglrc);

		[DllImport("opengl32.dll")]
		internal static extern void glClearColor(float red, float green, float blue, float alpha);

		[DllImport("opengl32.dll")]
		internal static extern void glClear(int mask);

		[DllImport("opengl32.dll")]
		internal static extern void glViewport(int x, int y, int width, int height);

		[DllImport("opengl32.dll")]
		internal static extern void glBegin(int mode);

		[DllImport("opengl32.dll")]
		internal static extern void glEnd();

		[DllImport("opengl32.dll")]
		internal static extern void glFlush();

		[DllImport("opengl32.dll")]
		internal static extern void glFinish();

		[DllImport("opengl32.dll")]
		internal static extern void glColor3f(float red, float green, float blue);

		[DllImport("opengl32.dll")]
		internal static extern void glVertex3f(float x, float y, float z);

		[DllImport("opengl32.dll")]
		internal static extern void SwapBuffers(IntPtr hdc);

		[DllImport("opengl32.dll")]
		internal static extern bool wglSwapLayerBuffers(IntPtr hdc, uint fuPlanes);

		[DllImport("opengl32.dll", SetLastError = true)]
		private static extern IntPtr glGetString(int name);

		[DllImport("opengl32.dll")]
		internal static extern void glReadPixels(int x, int y, int width, int height, int format, int type, IntPtr pixels);

		[DllImport("opengl32.dll")]
		public static extern void glGetIntegerv(int pname, out int data);

		public static string? GetOpenGLVersion()
			=> Marshal.PtrToStringAnsi(glGetString(GL_VERSION));

		[StructLayout(LayoutKind.Sequential)]
		internal struct PIXELFORMATDESCRIPTOR
		{
			public ushort nSize;
			public ushort nVersion;
			public uint dwFlags;
			public byte iPixelType;
			public byte cColorBits;
			public byte cRedBits;
			public byte cRedShift;
			public byte cGreenBits;
			public byte cGreenShift;
			public byte cBlueBits;
			public byte cBlueShift;
			public byte cAlphaBits;
			public byte cAlphaShift;
			public byte cAccumBits;
			public byte cAccumRedBits;
			public byte cAccumGreenBits;
			public byte cAccumBlueBits;
			public byte cAccumAlphaBits;
			public byte cDepthBits;
			public byte cStencilBits;
			public byte cAuxBuffers;
			public byte iLayerType;
			public byte bReserved;
			public uint dwLayerMask;
			public uint dwVisibleMask;
			public uint dwDamageMask;
		}

		internal const int PFD_DRAW_TO_WINDOW = 0x00000004;
		internal const int PFD_SUPPORT_OPENGL = 0x00000020;
		internal const int PFD_DOUBLEBUFFER = 0x00000001;
		internal const int PFD_TYPE_RGBA = 0;

		internal const int PFD_MAIN_PLANE = 0;
		internal const int WGL_SWAP_MAIN_PLANE = 1;

		internal const int GL_COLOR_BUFFER_BIT = 0x00004000;

		internal const int GL_TRIANGLES = 0x0004;

		internal const int GL_VERSION = 0x1F02;

		internal const int ERROR_SUCCESS = 0;

		internal const int GL_DEPTH_BUFFER_BIT = 0x00000100;
		internal const int GL_STENCIL_BUFFER_BIT = 0x400;

		internal const int GL_PROJECTION = 0x1701;
		internal const int GL_MODELVIEW = 0x1700;

		internal const int GL_BGRA_EXT = 0x80E1;
		internal const int GL_UNSIGNED_BYTE = 0x1401;

		internal const int GL_FRAMEBUFFER_BINDING = 0x8CA6;
		internal const int GL_STENCIL_BITS = 0x0D57;
		internal const int GL_STENCIL = 0x1802;
		internal const int GL_SAMPLES = 0x80A9;
	}
}
