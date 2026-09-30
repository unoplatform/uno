#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// An offscreen framebuffer a GL host composes into and blits onto the default framebuffer at present, so the
/// previous frame's pixels survive the swap and the compositor can repaint only the damaged region. A GL swap
/// leaves the default framebuffer undefined, which is why it cannot be composed into directly.
/// <para>Entry points are resolved through the host's own loader, so one implementation serves every GL host.
/// <see cref="IsSupported"/> is false when the context is too old to blit between framebuffers (GL/GLES 2), and
/// the host then hands over the default framebuffer as before and reports that it preserves nothing.</para>
/// </summary>
internal sealed class GLRetainedFramebuffer : IDisposable
{
	private const uint GL_TEXTURE_2D = 0x0DE1;
	private const uint GL_UNSIGNED_BYTE = 0x1401;
	private const uint GL_RGBA = 0x1908;
	private const uint GL_RGBA8 = 0x8058;
	private const uint GL_NEAREST = 0x2600;
	private const uint GL_TEXTURE_MAG_FILTER = 0x2800;
	private const uint GL_TEXTURE_MIN_FILTER = 0x2801;
	private const uint GL_TEXTURE_WRAP_S = 0x2802;
	private const uint GL_TEXTURE_WRAP_T = 0x2803;
	private const uint GL_CLAMP_TO_EDGE = 0x812F;
	private const uint GL_DEPTH24_STENCIL8 = 0x88F0;
	private const uint GL_DEPTH_STENCIL_ATTACHMENT = 0x821A;
	private const uint GL_COLOR_ATTACHMENT0 = 0x8CE0;
	private const uint GL_FRAMEBUFFER = 0x8D40;
	private const uint GL_RENDERBUFFER = 0x8D41;
	private const uint GL_READ_FRAMEBUFFER = 0x8CA8;
	private const uint GL_DRAW_FRAMEBUFFER = 0x8CA9;
	private const uint GL_FRAMEBUFFER_COMPLETE = 0x8CD5;
	private const uint GL_COLOR_BUFFER_BIT = 0x4000;
	private const uint GL_VERSION = 0x1F02;

	private delegate void GenDel(int n, uint[] ids);
	private delegate void DeleteDel(int n, uint[] ids);
	private delegate void BindDel(uint target, uint id);
	private delegate void TexImage2DDel(uint target, int level, int internalFormat, int width, int height, int border, uint format, uint type, IntPtr pixels);
	private delegate void TexParameteriDel(uint target, uint name, int value);
	private delegate void FramebufferTexture2DDel(uint target, uint attachment, uint texTarget, uint texture, int level);
	private delegate void RenderbufferStorageDel(uint target, uint internalFormat, int width, int height);
	private delegate void FramebufferRenderbufferDel(uint target, uint attachment, uint rbTarget, uint renderbuffer);
	private delegate uint CheckFramebufferStatusDel(uint target);
	private delegate void BlitFramebufferDel(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, uint mask, uint filter);
	private delegate IntPtr GetStringDel(uint name);

	private GenDel? _genFramebuffers;
	private DeleteDel? _deleteFramebuffers;
	private BindDel? _bindFramebuffer;
	private GenDel? _genTextures;
	private DeleteDel? _deleteTextures;
	private BindDel? _bindTexture;
	private TexImage2DDel? _texImage2D;
	private TexParameteriDel? _texParameteri;
	private FramebufferTexture2DDel? _framebufferTexture2D;
	private GenDel? _genRenderbuffers;
	private DeleteDel? _deleteRenderbuffers;
	private BindDel? _bindRenderbuffer;
	private RenderbufferStorageDel? _renderbufferStorage;
	private FramebufferRenderbufferDel? _framebufferRenderbuffer;
	private CheckFramebufferStatusDel? _checkFramebufferStatus;
	private BlitFramebufferDel? _blitFramebuffer;

	private uint _framebuffer;
	private uint _texture;
	private uint _depthStencil;
	private int _width;
	private int _height;

	private readonly Func<string, nint> _getProcAddress;
	private bool _loaded;

	public GLRetainedFramebuffer(Func<string, nint> getProcAddress) => _getProcAddress = getProcAddress;

	// Resolved on first use rather than in the constructor: reading GL_VERSION needs the context current, which the
	// host only guarantees once it is acquiring a frame.
	private void EnsureLoaded()
	{
		if (_loaded)
		{
			return;
		}

		_loaded = true;
		var getProcAddress = _getProcAddress;

		T? Load<T>(string name) where T : Delegate
		{
			var address = getProcAddress(name);
			return address == 0 ? null : Marshal.GetDelegateForFunctionPointer<T>(address);
		}

		_genFramebuffers = Load<GenDel>("glGenFramebuffers");
		_deleteFramebuffers = Load<DeleteDel>("glDeleteFramebuffers");
		_bindFramebuffer = Load<BindDel>("glBindFramebuffer");
		_genTextures = Load<GenDel>("glGenTextures");
		_deleteTextures = Load<DeleteDel>("glDeleteTextures");
		_bindTexture = Load<BindDel>("glBindTexture");
		_texImage2D = Load<TexImage2DDel>("glTexImage2D");
		_texParameteri = Load<TexParameteriDel>("glTexParameteri");
		_framebufferTexture2D = Load<FramebufferTexture2DDel>("glFramebufferTexture2D");
		_genRenderbuffers = Load<GenDel>("glGenRenderbuffers");
		_deleteRenderbuffers = Load<DeleteDel>("glDeleteRenderbuffers");
		_bindRenderbuffer = Load<BindDel>("glBindRenderbuffer");
		_renderbufferStorage = Load<RenderbufferStorageDel>("glRenderbufferStorage");
		_framebufferRenderbuffer = Load<FramebufferRenderbufferDel>("glFramebufferRenderbuffer");
		_checkFramebufferStatus = Load<CheckFramebufferStatusDel>("glCheckFramebufferStatus");
		_blitFramebuffer = Load<BlitFramebufferDel>("glBlitFramebuffer");

		// A loader may hand back an entry point the current context cannot actually run (eglGetProcAddress is not
		// required to return null for one), so the version is the authority on whether blitting exists at all.
		var version = Load<GetStringDel>("glGetString") is { } getString ? Marshal.PtrToStringAnsi(getString(GL_VERSION)) : null;

		IsSupported = HasFramebufferBlit(version)
			&& _genFramebuffers is not null && _deleteFramebuffers is not null && _bindFramebuffer is not null
			&& _genTextures is not null && _deleteTextures is not null && _bindTexture is not null
			&& _texImage2D is not null && _texParameteri is not null && _framebufferTexture2D is not null
			&& _genRenderbuffers is not null && _deleteRenderbuffers is not null && _bindRenderbuffer is not null
			&& _renderbufferStorage is not null && _framebufferRenderbuffer is not null
			&& _checkFramebufferStatus is not null && _blitFramebuffer is not null;

		if (!IsSupported)
		{
			this.Log().Debug("The GL context cannot blit between framebuffers (GL/GLES 2); every frame will repaint whole.");
		}
	}

	/// <summary>False when the context is too old to retain, in which case the host must compose into the default
	/// framebuffer and report that nothing is preserved. Meaningful only after the first <see cref="TryResize"/>.</summary>
	public bool IsSupported { get; private set; }

	/// <summary>
	/// Blitting between framebuffers arrived in GL 3.0 and GLES 3.0. GL_VERSION is "<major>.<minor>…" on desktop
	/// and "OpenGL ES <major>.<minor>…" on ES; an unreadable string is treated as too old.
	/// </summary>
	private static bool HasFramebufferBlit(string? version)
	{
		if (string.IsNullOrEmpty(version))
		{
			return false;
		}

		var span = version.AsSpan().Trim();
		if (span.StartsWith("OpenGL ES"))
		{
			span = span["OpenGL ES".Length..].Trim();
		}

		var dot = span.IndexOf('.');
		return dot > 0 && int.TryParse(span[..dot], out var major) && major >= 3;
	}

	/// <summary>The retained framebuffer's name, valid only after a successful <see cref="TryResize"/>.</summary>
	public uint FramebufferId => _framebuffer;

	/// <summary>The stencil the attached packed depth/stencil buffer provides, which the backend needs for clipping.</summary>
	public int StencilBits => 8;

	/// <summary>
	/// Sizes the retained framebuffer for the frame about to be composed. Returns false when there is nothing to
	/// compose into (unsupported, or allocation failed), and reports through <paramref name="contentsPreserved"/>
	/// whether it still holds the previous frame — a freshly allocated one holds nothing, so that frame must repaint
	/// whole even though the host is otherwise retaining.
	/// </summary>
	public bool TryResize(int width, int height, out bool contentsPreserved)
	{
		contentsPreserved = false;
		EnsureLoaded();
		if (!IsSupported)
		{
			return false;
		}

		width = Math.Max(1, width);
		height = Math.Max(1, height);
		if (_framebuffer != 0 && width == _width && height == _height)
		{
			contentsPreserved = true;
			return true;
		}

		Release();

		var ids = new uint[1];
		_genFramebuffers!(1, ids);
		_framebuffer = ids[0];
		_genTextures!(1, ids);
		_texture = ids[0];
		_genRenderbuffers!(1, ids);
		_depthStencil = ids[0];

		_bindTexture!(GL_TEXTURE_2D, _texture);
		_texImage2D!(GL_TEXTURE_2D, 0, (int)GL_RGBA8, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);
		_texParameteri!(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_NEAREST);
		_texParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_NEAREST);
		_texParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
		_texParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);
		_bindTexture(GL_TEXTURE_2D, 0);

		_bindRenderbuffer!(GL_RENDERBUFFER, _depthStencil);
		_renderbufferStorage!(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, width, height);
		_bindRenderbuffer(GL_RENDERBUFFER, 0);

		_bindFramebuffer!(GL_FRAMEBUFFER, _framebuffer);
		_framebufferTexture2D!(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, _texture, 0);
		_framebufferRenderbuffer!(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_RENDERBUFFER, _depthStencil);
		var status = _checkFramebufferStatus!(GL_FRAMEBUFFER);
		_bindFramebuffer(GL_FRAMEBUFFER, 0);

		if (status != GL_FRAMEBUFFER_COMPLETE)
		{
			this.Log().Warn($"The retained {width}x{height} framebuffer is incomplete (0x{status:X}); every frame will repaint whole.");
			Release();
			return false;
		}

		_width = width;
		_height = height;
		return true;
	}

	/// <summary>Copies the composed frame onto the default framebuffer, which the host then swaps.</summary>
	public void BlitToDefault()
	{
		if (_framebuffer == 0)
		{
			return;
		}

		_bindFramebuffer!(GL_READ_FRAMEBUFFER, _framebuffer);
		_bindFramebuffer(GL_DRAW_FRAMEBUFFER, 0);
		// Same size and both bottom-left origin, so a 1:1 copy needs no filtering.
		_blitFramebuffer!(0, 0, _width, _height, 0, 0, _width, _height, GL_COLOR_BUFFER_BIT, GL_NEAREST);
		_bindFramebuffer(GL_FRAMEBUFFER, 0);
	}

	private void Release()
	{
		if (!_loaded)
		{
			return;
		}

		var ids = new uint[1];
		if (_framebuffer != 0) { ids[0] = _framebuffer; _deleteFramebuffers!(1, ids); _framebuffer = 0; }
		if (_texture != 0) { ids[0] = _texture; _deleteTextures!(1, ids); _texture = 0; }
		if (_depthStencil != 0) { ids[0] = _depthStencil; _deleteRenderbuffers!(1, ids); _depthStencil = 0; }
		_width = 0;
		_height = 0;
	}

	public void Dispose() => Release();
}
