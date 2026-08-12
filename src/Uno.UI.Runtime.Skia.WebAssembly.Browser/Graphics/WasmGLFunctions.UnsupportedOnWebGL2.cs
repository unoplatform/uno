#nullable enable

using System.Collections.Generic;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics;

// Registry of GL functions that have no WebGL2 equivalent. When user code (or Silk.NET probing)
// asks for one of these, WasmGLFunctions.GetProcAddress logs an explicit "not available on
// WebGL2" error instead of failing silently. Function names not in this map AND not in the
// implemented dispatch table get a generic 'No function was found' from Silk.NET - that path
// is used for benign extension probes.
internal static unsafe partial class WasmGLFunctions
{
	private static readonly Dictionary<string, string> _unsupportedOnWebGL2 = new(System.StringComparer.Ordinal)
	{
		// Compute shaders (GLES 3.1, not in WebGL2)
		["glDispatchCompute"] = "compute shaders aren't in WebGL2",
		["glDispatchComputeIndirect"] = "compute shaders aren't in WebGL2",
		["glMemoryBarrier"] = "compute-shader memory barriers aren't in WebGL2",
		["glMemoryBarrierByRegion"] = "compute-shader memory barriers aren't in WebGL2",
		["glBindImageTexture"] = "image load/store isn't in WebGL2",

		// Tessellation / geometry shaders
		["glPatchParameteri"] = "tessellation isn't in WebGL2",
		["glPatchParameterfv"] = "tessellation isn't in WebGL2",
		["glFramebufferTexture"] = "geometry shaders aren't in WebGL2",

		// Direct State Access (DSA, GL 4.5+, not in GLES/WebGL2)
		["glCreateBuffers"] = "DSA isn't in WebGL2 (use glGenBuffers)",
		["glCreateTextures"] = "DSA isn't in WebGL2 (use glGenTextures)",
		["glCreateFramebuffers"] = "DSA isn't in WebGL2 (use glGenFramebuffers)",
		["glCreateRenderbuffers"] = "DSA isn't in WebGL2 (use glGenRenderbuffers)",
		["glCreateVertexArrays"] = "DSA isn't in WebGL2 (use glGenVertexArrays)",
		["glCreateSamplers"] = "DSA isn't in WebGL2 (use glGenSamplers)",
		["glCreateProgramPipelines"] = "separable shaders aren't in WebGL2",
		["glCreateQueries"] = "DSA isn't in WebGL2 (use glGenQueries)",
		["glCreateTransformFeedbacks"] = "DSA isn't in WebGL2 (use glGenTransformFeedbacks)",
		["glNamedBufferData"] = "DSA isn't in WebGL2 (use glBufferData on the bound buffer)",
		["glNamedBufferSubData"] = "DSA isn't in WebGL2 (use glBufferSubData on the bound buffer)",
		["glTextureStorage2D"] = "DSA isn't in WebGL2 (use glTexStorage2D on the bound texture)",
		["glTextureSubImage2D"] = "DSA isn't in WebGL2 (use glTexSubImage2D on the bound texture)",
		["glNamedFramebufferTexture"] = "DSA isn't in WebGL2 (use glFramebufferTexture2D on the bound FBO)",
		["glNamedFramebufferRenderbuffer"] = "DSA isn't in WebGL2 (use glFramebufferRenderbuffer on the bound FBO)",
		["glNamedRenderbufferStorage"] = "DSA isn't in WebGL2 (use glRenderbufferStorage on the bound renderbuffer)",
		["glClearNamedFramebufferfv"] = "DSA isn't in WebGL2 (use glClearBufferfv on the bound FBO)",
		["glClearNamedFramebufferiv"] = "DSA isn't in WebGL2 (use glClearBufferiv on the bound FBO)",
		["glClearNamedFramebufferuiv"] = "DSA isn't in WebGL2 (use glClearBufferuiv on the bound FBO)",
		["glClearNamedFramebufferfi"] = "DSA isn't in WebGL2 (use glClearBufferfi on the bound FBO)",

		// Multi-draw indirect / base vertex
		["glMultiDrawArrays"] = "multi-draw isn't in WebGL2",
		["glMultiDrawElements"] = "multi-draw isn't in WebGL2",
		["glDrawElementsBaseVertex"] = "drawElementsBaseVertex isn't in WebGL2",
		["glDrawRangeElementsBaseVertex"] = "drawRangeElementsBaseVertex isn't in WebGL2",
		["glDrawElementsInstancedBaseVertex"] = "drawElementsInstancedBaseVertex isn't in WebGL2",
		["glDrawArraysIndirect"] = "indirect draw isn't in WebGL2",
		["glDrawElementsIndirect"] = "indirect draw isn't in WebGL2",

		// glGetTexImage - WebGL2 can't read texture contents directly (attach to FBO + readPixels)
		["glGetTexImage"] = "WebGL2 has no glGetTexImage; attach the texture to an FBO and use glReadPixels",
		["glGetnTexImage"] = "WebGL2 has no glGetnTexImage; attach the texture to an FBO and use glReadPixels",
		["glGetCompressedTexImage"] = "WebGL2 has no glGetCompressedTexImage",

		// Immutable storage / explicit buffer storage
		["glBufferStorage"] = "glBufferStorage isn't in WebGL2 (use glBufferData)",
		["glCopyImageSubData"] = "glCopyImageSubData isn't in WebGL2",

		// Debug output (GLES extension, WebGL uses WEBGL_debug_renderer_info instead)
		["glDebugMessageControl"] = "GL debug output isn't in WebGL2 (use the WEBGL_debug_* extensions)",
		["glDebugMessageInsert"] = "GL debug output isn't in WebGL2",
		["glDebugMessageCallback"] = "GL debug output isn't in WebGL2",
		["glGetDebugMessageLog"] = "GL debug output isn't in WebGL2",
		["glPushDebugGroup"] = "GL debug groups aren't in WebGL2",
		["glPopDebugGroup"] = "GL debug groups aren't in WebGL2",
		["glObjectLabel"] = "GL object labels aren't in WebGL2",
		["glGetObjectLabel"] = "GL object labels aren't in WebGL2",

		// Polygon mode (only FILL in WebGL2)
		["glPolygonMode"] = "WebGL2 only supports FILL polygon mode",

		// Immediate mode / fixed-function (long removed from core, never in GLES/WebGL)
		["glBegin"] = "immediate mode isn't in WebGL2",
		["glEnd"] = "immediate mode isn't in WebGL2",
		["glVertex3f"] = "immediate mode isn't in WebGL2",
		["glNormal3f"] = "immediate mode isn't in WebGL2",
		["glTexCoord2f"] = "immediate mode isn't in WebGL2",
		["glColor3f"] = "immediate mode isn't in WebGL2",
		["glColor4f"] = "immediate mode isn't in WebGL2",

		// Matrix stack / fixed-function transforms
		["glMatrixMode"] = "fixed-function transforms aren't in WebGL2; use shader uniforms",
		["glLoadIdentity"] = "fixed-function transforms aren't in WebGL2",
		["glLoadMatrixf"] = "fixed-function transforms aren't in WebGL2",
		["glMultMatrixf"] = "fixed-function transforms aren't in WebGL2",
		["glPushMatrix"] = "fixed-function transforms aren't in WebGL2",
		["glPopMatrix"] = "fixed-function transforms aren't in WebGL2",
		["glTranslatef"] = "fixed-function transforms aren't in WebGL2",
		["glRotatef"] = "fixed-function transforms aren't in WebGL2",
		["glScalef"] = "fixed-function transforms aren't in WebGL2",
		["glOrtho"] = "fixed-function transforms aren't in WebGL2",
		["glFrustum"] = "fixed-function transforms aren't in WebGL2",

		// Other deprecated state
		["glLineStipple"] = "line stipple isn't in WebGL2",
		["glPolygonStipple"] = "polygon stipple isn't in WebGL2",
		["glPointSize"] = "WebGL2 has no glPointSize; write gl_PointSize in the vertex shader",
		["glDrawPixels"] = "glDrawPixels isn't in WebGL2",
		["glRasterPos2f"] = "raster position isn't in WebGL2",
		["glBitmap"] = "glBitmap isn't in WebGL2",
		["glAccum"] = "accumulation buffer isn't in WebGL2",
		["glClearAccum"] = "accumulation buffer isn't in WebGL2",
	};
}
