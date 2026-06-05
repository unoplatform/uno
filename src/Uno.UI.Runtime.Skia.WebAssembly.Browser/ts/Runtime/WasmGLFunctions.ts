// JS WebGL2 shim called from C# WasmGLFunctions.cs via [JSImport].
// Pointer args are byte offsets into Module.HEAP*; we decode them here.
// Object handles (programs/shaders/buffers/...) follow emscripten's library_webgl.js
// conventions so they coexist with anything window.GL has already touched.
namespace Uno.UI.Runtime.Skia {

	type GlAny = any;

	function gl(): WebGL2RenderingContext {
		const ctx = (<GlAny>window).GL.currentContext;
		if (!ctx || !ctx.GLctx) {
			throw "WasmGLFunctions: no current WebGL context. Did MakeCurrent run?";
		}
		return ctx.GLctx as WebGL2RenderingContext;
	}

	function tables(): GlAny {
		const G = (<GlAny>window).GL;
		// Lazily ensure each handle table exists. Emscripten's library_webgl.js creates the
		// common tables (buffers, programs, ...) at startup, but the WebGL2-era ones (syncs,
		// transformFeedbacks, ...) vary by emscripten version and link configuration.
		G.buffers ||= [null];
		G.programs ||= [null];
		G.shaders ||= [null];
		G.textures ||= [null];
		G.vaos ||= [null];
		G.framebuffers ||= [null];
		G.renderbuffers ||= [null];
		G.uniforms ||= [null];
		G.samplers ||= [null];
		G.queries ||= [null];
		G.syncs ||= [null];
		G.transformFeedbacks ||= [null];
		return G;
	}

	function getNewId(table: any[]): number {
		const G = (<GlAny>window).GL;
		if (typeof G.getNewId === "function") {
			return G.getNewId(table);
		}
		// Fallback: linear scan.
		for (let i = 1; i < table.length; i++) {
			if (!table[i]) return i;
		}
		return table.length;
	}

	function readUInt32Array(ptr: number, count: number): Uint32Array {
		// Copy out so we don't depend on heap stability across re-grow.
		return new Uint32Array((<GlAny>window).Module.HEAPU32.buffer, ptr, count).slice();
	}

	function writeUInt32(ptr: number, value: number): void {
		(<GlAny>window).Module.HEAPU32[ptr >> 2] = value;
	}

	function writeInt32(ptr: number, value: number): void {
		(<GlAny>window).Module.HEAP32[ptr >> 2] = value;
	}

	function readCString(ptr: number, maxLen: number = -1): string {
		const HEAPU8 = (<GlAny>window).Module.HEAPU8 as Uint8Array;
		let end = ptr;
		if (maxLen < 0) {
			while (HEAPU8[end] !== 0) end++;
		} else {
			end = ptr + maxLen;
		}
		const bytes = HEAPU8.subarray(ptr, end);
		return new TextDecoder("utf-8").decode(bytes);
	}

	function writeCString(ptr: number, bufSize: number, str: string, lengthOutPtr: number): void {
		if (bufSize <= 0) {
			if (lengthOutPtr !== 0) writeInt32(lengthOutPtr, 0);
			return;
		}
		const HEAPU8 = (<GlAny>window).Module.HEAPU8 as Uint8Array;
		const bytes = new TextEncoder().encode(str);
		const writable = Math.min(bytes.length, bufSize - 1);
		HEAPU8.set(bytes.subarray(0, writable), ptr);
		HEAPU8[ptr + writable] = 0;
		if (lengthOutPtr !== 0) writeInt32(lengthOutPtr, writable);
	}

	// One-shot portability warnings: emitted the first time a WebGL2-incompatible state
	// combination is detected, so users get an explanation instead of only the per-draw
	// INVALID_OPERATION spam the browser produces.
	const warnedOnce = new Set<string>();
	function warnOnce(key: string, message: string): void {
		if (!warnedOnce.has(key)) {
			warnedOnce.add(key);
			console.warn(message);
		}
	}

	// WebGL2 (unlike desktop GL) fails every draw with INVALID_OPERATION while the front and
	// back stencil reference/value-mask/write-mask differ; only the funcs/ops may vary per face.
	// The mismatch is recorded when stencil state is set (transient divergence between two
	// consecutive Separate calls is legitimate) and only reported at draw time while the
	// stencil test is enabled.
	function recordStencilMismatch(ctx: WebGL2RenderingContext): void {
		(<GlAny>ctx).__unoStencilMismatch =
			ctx.getParameter(ctx.STENCIL_REF) !== ctx.getParameter(ctx.STENCIL_BACK_REF)
			|| ctx.getParameter(ctx.STENCIL_VALUE_MASK) !== ctx.getParameter(ctx.STENCIL_BACK_VALUE_MASK)
			|| ctx.getParameter(ctx.STENCIL_WRITEMASK) !== ctx.getParameter(ctx.STENCIL_BACK_WRITEMASK);
	}

	function warnIfStencilMismatch(ctx: WebGL2RenderingContext): void {
		if ((<GlAny>ctx).__unoStencilMismatch && ctx.isEnabled(ctx.STENCIL_TEST)) {
			warnOnce("stencilMismatch",
				"Draw call with diverging front/back stencil reference, value mask, or write mask. " +
				"WebGL2 requires them to be identical (only the stencil funcs/ops may differ per face); " +
				"draws fail with INVALID_OPERATION until they match again.");
		}
	}

	// Static C-string cache for glGetString. Each unique pname value gets a single allocation
	// in wasm memory that we never free, matching glGetString's "pointer remains valid" contract.
	const stringCache: { [pname: number]: number } = {};

	function allocCString(s: string): number {
		const bytes = new TextEncoder().encode(s);
		const ptr = (<GlAny>window).Module._malloc(bytes.length + 1);
		const HEAPU8 = (<GlAny>window).Module.HEAPU8 as Uint8Array;
		HEAPU8.set(bytes, ptr);
		HEAPU8[ptr + bytes.length] = 0;
		return ptr;
	}

	export class WasmGLFunctions {

		// ------------------------------------------------------------------------------------
		// State / queries
		// ------------------------------------------------------------------------------------

		public static glGetString(name: number): number {
			const cached = stringCache[name];
			if (cached) return cached;
			const ctx = gl();
			let value: string;
			try {
				value = ctx.getParameter(name) as string;
				if (value == null) value = "";
			} catch {
				value = "";
			}
			const ptr = allocCString(value);
			stringCache[name] = ptr;
			return ptr;
		}

		public static glGetIntegerv(pname: number, dataPtr: number): void {
			// GL_MAJOR_VERSION (0x821B) and GL_MINOR_VERSION (0x821C) aren't queryable through
			// WebGL2's getParameter (they were added to desktop GL/GLES core but never reflected
			// into WebGL). Parse them out of the VERSION string instead. WebGL 2.0 corresponds to
			// GLES 3.0 by spec; WebGL 1.0 corresponds to GLES 2.0.
			if (pname === 0x821B || pname === 0x821C) {
				const ctx = gl();
				const versionStr = (ctx.getParameter(ctx.VERSION) as string) || "";
				let major = 0;
				let minor = 0;
				const esMatch = versionStr.match(/OpenGL ES (\d+)\.(\d+)/);
				if (esMatch) {
					major = parseInt(esMatch[1], 10);
					minor = parseInt(esMatch[2], 10);
				} else {
					const webglMatch = versionStr.match(/WebGL (\d+)\.(\d+)/);
					if (webglMatch) {
						const wgMajor = parseInt(webglMatch[1], 10);
						if (wgMajor >= 2) { major = 3; minor = 0; }
						else { major = 2; minor = 0; }
					}
				}
				writeInt32(dataPtr, pname === 0x821B ? major : minor);
				return;
			}

			// Object-binding queries (FRAMEBUFFER_BINDING, RENDERBUFFER_BINDING, BUFFER_*_BINDING,
			// TEXTURE_BINDING_*, VERTEX_ARRAY_BINDING, CURRENT_PROGRAM) return the bound WebGL*
			// object - or null when nothing's bound. Reverse-map to the integer id we stamped onto
			// them in glGen*. Look the object up in the table as a fallback in case .name wasn't
			// set (e.g. objects auto-created on first bind by external code).
			if (pname === 0x8CA6 /* FRAMEBUFFER_BINDING */ || pname === 0x8CA7 /* RENDERBUFFER_BINDING */ ||
				pname === 0x8894 /* ARRAY_BUFFER_BINDING */ || pname === 0x8895 /* ELEMENT_ARRAY_BUFFER_BINDING */ ||
				pname === 0x8C8F /* TRANSFORM_FEEDBACK_BUFFER_BINDING */ || pname === 0x8A28 /* UNIFORM_BUFFER_BINDING */ ||
				pname === 0x85B5 /* VERTEX_ARRAY_BINDING */ || pname === 0x8069 /* TEXTURE_BINDING_2D */ ||
				pname === 0x8514 /* TEXTURE_BINDING_CUBE_MAP */ || pname === 0x8B8D /* CURRENT_PROGRAM */) {
				const obj = gl().getParameter(pname);
				if (!obj) { writeInt32(dataPtr, 0); return; }
				let id = ((obj as any).name | 0);
				if (!id) {
					const candidates: any[][] = [
						tables().framebuffers, tables().renderbuffers, tables().buffers,
						tables().textures, tables().vaos, tables().programs,
					];
					for (const table of candidates) {
						for (let i = 1; i < table.length; i++) {
							if (table[i] === obj) { id = i; break; }
						}
						if (id) break;
					}
				}
				writeInt32(dataPtr, id);
				return;
			}

			const result = gl().getParameter(pname);
			if (typeof result === "number") {
				writeInt32(dataPtr, result | 0);
			} else if (typeof result === "boolean") {
				writeInt32(dataPtr, result ? 1 : 0);
			} else if (result && typeof (result as any).length === "number") {
				for (let i = 0; i < (result as any).length; i++) {
					writeInt32(dataPtr + i * 4, (result as any)[i] | 0);
				}
			} else {
				writeInt32(dataPtr, 0);
			}
		}

		public static glGetError(): number {
			return gl().getError();
		}

		public static glViewport(x: number, y: number, width: number, height: number): void {
			gl().viewport(x, y, width, height);
		}

		public static glEnable(cap: number): void { gl().enable(cap); }
		public static glDisable(cap: number): void { gl().disable(cap); }
		public static glClear(mask: number): void { gl().clear(mask); }
		public static glClearColor(r: number, g: number, b: number, a: number): void { gl().clearColor(r, g, b, a); }

		public static glDrawArrays(mode: number, first: number, count: number): void {
			const ctx = gl();
			warnIfStencilMismatch(ctx);
			ctx.drawArrays(mode, first, count);
		}

		public static glDrawElements(mode: number, count: number, type: number, indicesPtr: number): void {
			const ctx = gl();
			warnIfStencilMismatch(ctx);
			ctx.drawElements(mode, count, type, indicesPtr);
		}

		public static glPixelStorei(pname: number, param: number): void { gl().pixelStorei(pname, param); }

		// ------------------------------------------------------------------------------------
		// Buffers
		// ------------------------------------------------------------------------------------

		public static glGenBuffers(n: number, buffersPtr: number): void {
			const ctx = gl();
			const t = tables();
			for (let i = 0; i < n; i++) {
				const buf = ctx.createBuffer() as GlAny;
				const id = getNewId(t.buffers);
				if (buf) buf.name = id;
				t.buffers[id] = buf;
				writeUInt32(buffersPtr + i * 4, id);
			}
		}

		public static glDeleteBuffers(n: number, buffersPtr: number): void {
			const ctx = gl();
			const t = tables();
			const ids = readUInt32Array(buffersPtr, n);
			for (let i = 0; i < n; i++) {
				const id = ids[i];
				const buf = t.buffers[id];
				if (buf) {
					ctx.deleteBuffer(buf);
					t.buffers[id] = null;
				}
			}
		}

		public static glBindBuffer(target: number, buffer: number): void {
			gl().bindBuffer(target, buffer === 0 ? null : tables().buffers[buffer]);
		}

		public static glBufferData(target: number, size: number, dataPtr: number, usage: number): void {
			const ctx = gl();
			if (dataPtr === 0) {
				ctx.bufferData(target, size, usage);
			} else {
				const HEAPU8 = (<GlAny>window).Module.HEAPU8 as Uint8Array;
				ctx.bufferData(target, HEAPU8.subarray(dataPtr, dataPtr + size), usage);
			}
		}

		// ------------------------------------------------------------------------------------
		// Vertex arrays
		// ------------------------------------------------------------------------------------

		public static glGenVertexArrays(n: number, arraysPtr: number): void {
			const ctx = gl();
			const t = tables();
			for (let i = 0; i < n; i++) {
				const vao = ctx.createVertexArray() as GlAny;
				const id = getNewId(t.vaos);
				if (vao) vao.name = id;
				t.vaos[id] = vao;
				writeUInt32(arraysPtr + i * 4, id);
			}
		}

		public static glDeleteVertexArrays(n: number, arraysPtr: number): void {
			const ctx = gl();
			const t = tables();
			const ids = readUInt32Array(arraysPtr, n);
			for (let i = 0; i < n; i++) {
				const id = ids[i];
				const vao = t.vaos[id];
				if (vao) {
					ctx.deleteVertexArray(vao);
					t.vaos[id] = null;
				}
			}
		}

		public static glBindVertexArray(array: number): void {
			gl().bindVertexArray(array === 0 ? null : tables().vaos[array]);
		}

		public static glVertexAttribPointer(index: number, size: number, type: number, normalized: number, stride: number, pointer: number): void {
			gl().vertexAttribPointer(index, size, type, normalized !== 0, stride, pointer);
		}

		public static glEnableVertexAttribArray(index: number): void { gl().enableVertexAttribArray(index); }
		public static glDisableVertexAttribArray(index: number): void { gl().disableVertexAttribArray(index); }

		// ------------------------------------------------------------------------------------
		// Textures
		// ------------------------------------------------------------------------------------

		public static glGenTextures(n: number, texturesPtr: number): void {
			const ctx = gl();
			const t = tables();
			for (let i = 0; i < n; i++) {
				const tex = ctx.createTexture() as GlAny;
				const id = getNewId(t.textures);
				if (tex) tex.name = id;
				t.textures[id] = tex;
				writeUInt32(texturesPtr + i * 4, id);
			}
		}

		public static glDeleteTextures(n: number, texturesPtr: number): void {
			const ctx = gl();
			const t = tables();
			const ids = readUInt32Array(texturesPtr, n);
			for (let i = 0; i < n; i++) {
				const id = ids[i];
				const tex = t.textures[id];
				if (tex) {
					ctx.deleteTexture(tex);
					t.textures[id] = null;
				}
			}
		}

		public static glBindTexture(target: number, texture: number): void {
			gl().bindTexture(target, texture === 0 ? null : tables().textures[texture]);
		}

		public static glActiveTexture(texture: number): void { gl().activeTexture(texture); }

		public static glTexImage2D(target: number, level: number, internalformat: number, width: number, height: number, border: number, format: number, type: number, pixelsPtr: number): void {
			const ctx = gl();
			// WebGL2's color-renderable list contains only sized internal formats (RGB8, RGBA8...).
			// The framework passes the unsized RGB / RGBA which produces a texture that isn't
			// framebuffer-renderable -> FRAMEBUFFER_INCOMPLETE_ATTACHMENT. Auto-promote to the
			// matching sized format when type is UNSIGNED_BYTE (the only one we currently care
			// about; extend if other types start needing FBO-renderable textures).
			if (type === 0x1401 /* GL_UNSIGNED_BYTE */) {
				if (internalformat === 0x1907 /* GL_RGB */) internalformat = 0x8051 /* GL_RGB8 */;
				else if (internalformat === 0x1908 /* GL_RGBA */) internalformat = 0x8058 /* GL_RGBA8 */;
			}
			if (width === 0 || height === 0) {
				console.warn(`WasmGLFunctions.glTexImage2D: zero-sized texture (${width}x${height}) - will likely cause FRAMEBUFFER_INCOMPLETE_ATTACHMENT.`);
			}
			if (pixelsPtr === 0) {
				ctx.texImage2D(target, level, internalformat, width, height, border, format, type, null);
			} else {
				const HEAPU8 = (<GlAny>window).Module.HEAPU8 as Uint8Array;
				// 32 bpp upper bound; texImage2D ignores trailing bytes once it has w*h*bpp.
				const byteCount = width * height * 4;
				ctx.texImage2D(target, level, internalformat, width, height, border, format, type, HEAPU8.subarray(pixelsPtr, pixelsPtr + byteCount));
			}
		}

		public static glTexParameteri(target: number, pname: number, param: number): void {
			gl().texParameteri(target, pname, param);
		}

		public static glTexParameterIuiv(target: number, pname: number, paramsPtr: number): void {
			// WebGL2 doesn't expose the texParameterIuiv vector variant (it was desktop GL only).
			// Silk.NET resolves gl.TexParameterI(target, pname, uint) here with a 1-element params
			// buffer; forward the single value to the scalar texParameteri, which covers every
			// pname GLCanvasElement uses (TEXTURE_MIN_FILTER, TEXTURE_MAG_FILTER, etc.).
			const value = ((<GlAny>window).Module.HEAPU32 as Uint32Array)[paramsPtr >> 2] | 0;
			gl().texParameteri(target, pname, value);
		}

		public static glReadBuffer(mode: number): void { gl().readBuffer(mode); }

		public static glReadPixels(x: number, y: number, width: number, height: number, format: number, type: number, pixelsPtr: number): void {
			const HEAPU8 = (<GlAny>window).Module.HEAPU8 as Uint8Array;
			// WebGL2 doesn't accept BGRA in readPixels. GLCanvasElement.Render() asks for
			// BGRA/UNSIGNED_BYTE so the bytes drop straight into the WriteableBitmap's BGRA
			// buffer. Translate by reading RGBA and swapping R/B channels in place.
			const isBgraByte = format === 0x80E1 /* GL_BGRA */ && type === 0x1401 /* GL_UNSIGNED_BYTE */;
			const effectiveFormat = isBgraByte ? 0x1908 /* GL_RGBA */ : format;
			const byteCount = width * height * 4;
			const view = new Uint8Array(HEAPU8.buffer, pixelsPtr, byteCount);
			gl().readPixels(x, y, width, height, effectiveFormat, type, view);
			if (isBgraByte) {
				for (let i = 0; i + 3 < byteCount; i += 4) {
					const tmp = view[i];
					view[i] = view[i + 2];
					view[i + 2] = tmp;
				}
			}
		}

		// ------------------------------------------------------------------------------------
		// Framebuffers / renderbuffers
		// ------------------------------------------------------------------------------------

		public static glGenFramebuffers(n: number, framebuffersPtr: number): void {
			const ctx = gl();
			const t = tables();
			for (let i = 0; i < n; i++) {
				const fb = ctx.createFramebuffer() as GlAny;
				const id = getNewId(t.framebuffers);
				if (fb) fb.name = id;
				t.framebuffers[id] = fb;
				writeUInt32(framebuffersPtr + i * 4, id);
			}
		}

		public static glDeleteFramebuffers(n: number, framebuffersPtr: number): void {
			const ctx = gl();
			const t = tables();
			const ids = readUInt32Array(framebuffersPtr, n);
			for (let i = 0; i < n; i++) {
				const id = ids[i];
				const fb = t.framebuffers[id];
				if (fb) {
					ctx.deleteFramebuffer(fb);
					t.framebuffers[id] = null;
				}
			}
		}

		public static glBindFramebuffer(target: number, framebuffer: number): void {
			if (framebuffer === 0) {
				gl().bindFramebuffer(target, null);
				return;
			}
			// Desktop GL auto-creates FBOs on first bind for any unknown name; WebGL2 requires
			// an explicit createFramebuffer() first. GLCanvasElement.FrameBufferDetails relies
			// on the desktop semantics (creates an FBO via gl.GenBuffer, then binds the resulting
			// VBO id as a framebuffer name and lets the impl create-on-bind). Emulate that here
			// so the framework's existing pattern works unchanged.
			const t = tables();
			let fb = t.framebuffers[framebuffer];
			if (!fb) {
				fb = gl().createFramebuffer() as GlAny;
				if (fb) (fb as GlAny).name = framebuffer;
				t.framebuffers[framebuffer] = fb;
			}
			gl().bindFramebuffer(target, fb);
		}

		public static glCheckFramebufferStatus(target: number): number {
			const ctx = gl();
			const status = ctx.checkFramebufferStatus(target);
			if (status !== 0x8CD5 /* GL_FRAMEBUFFER_COMPLETE */) {
				const statusNames: { [k: number]: string } = {
					0x8CD6: "INCOMPLETE_ATTACHMENT",
					0x8CD7: "INCOMPLETE_MISSING_ATTACHMENT",
					0x8CD9: "INCOMPLETE_DIMENSIONS",
					0x8CDD: "UNSUPPORTED",
					0x8D56: "INCOMPLETE_MULTISAMPLE",
					0x8DA8: "INCOMPLETE_LAYER_TARGETS",
				};
				const name = statusNames[status] || `0x${status.toString(16)}`;
				const lastErr = ctx.getError();
				console.warn(`WasmGLFunctions: framebuffer incomplete = ${name} (0x${status.toString(16)}), most recent gl.getError = 0x${lastErr.toString(16)}`);
			}
			return status;
		}

		public static glFramebufferTexture2D(target: number, attachment: number, textarget: number, texture: number, level: number): void {
			gl().framebufferTexture2D(target, attachment, textarget, texture === 0 ? null : tables().textures[texture], level);
		}

		public static glFramebufferRenderbuffer(target: number, attachment: number, renderbuffertarget: number, renderbuffer: number): void {
			gl().framebufferRenderbuffer(target, attachment, renderbuffertarget, renderbuffer === 0 ? null : tables().renderbuffers[renderbuffer]);
		}

		public static glGenRenderbuffers(n: number, renderbuffersPtr: number): void {
			const ctx = gl();
			const t = tables();
			for (let i = 0; i < n; i++) {
				const rb = ctx.createRenderbuffer() as GlAny;
				const id = getNewId(t.renderbuffers);
				if (rb) rb.name = id;
				t.renderbuffers[id] = rb;
				writeUInt32(renderbuffersPtr + i * 4, id);
			}
		}

		public static glDeleteRenderbuffers(n: number, renderbuffersPtr: number): void {
			const ctx = gl();
			const t = tables();
			const ids = readUInt32Array(renderbuffersPtr, n);
			for (let i = 0; i < n; i++) {
				const id = ids[i];
				const rb = t.renderbuffers[id];
				if (rb) {
					ctx.deleteRenderbuffer(rb);
					t.renderbuffers[id] = null;
				}
			}
		}

		public static glBindRenderbuffer(target: number, renderbuffer: number): void {
			gl().bindRenderbuffer(target, renderbuffer === 0 ? null : tables().renderbuffers[renderbuffer]);
		}

		public static glRenderbufferStorage(target: number, internalformat: number, width: number, height: number): void {
			gl().renderbufferStorage(target, internalformat, width, height);
		}

		// ------------------------------------------------------------------------------------
		// Shaders / programs
		// ------------------------------------------------------------------------------------

		public static glCreateShader(type: number): number {
			const ctx = gl();
			const t = tables();
			const sh = ctx.createShader(type) as GlAny;
			if (!sh) return 0;
			const id = getNewId(t.shaders);
			sh.name = id;
			t.shaders[id] = sh;
			return id;
		}

		public static glDeleteShader(shader: number): void {
			const t = tables();
			const sh = t.shaders[shader];
			if (sh) {
				gl().deleteShader(sh);
				t.shaders[shader] = null;
			}
		}

		public static glShaderSource(shader: number, count: number, stringsPtr: number, lengthsPtr: number): void {
			const ctx = gl();
			const t = tables();
			const sh = t.shaders[shader];
			if (!sh) return;
			const ptrs = readUInt32Array(stringsPtr, count);
			const lens = lengthsPtr === 0 ? null : readUInt32Array(lengthsPtr, count);
			let source = "";
			for (let i = 0; i < count; i++) {
				const len = lens ? (lens[i] | 0) : -1;
				source += readCString(ptrs[i], len < 0 ? -1 : len);
			}
			ctx.shaderSource(sh, source);
		}

		public static glCompileShader(shader: number): void {
			const sh = tables().shaders[shader];
			if (sh) gl().compileShader(sh);
		}

		public static glGetShaderiv(shader: number, pname: number, paramsPtr: number): void {
			const sh = tables().shaders[shader];
			if (!sh) { writeInt32(paramsPtr, 0); return; }
			// WebGL2 dropped the *_LENGTH pnames (strings are returned directly); compute them.
			// Silk.NET's string-returning helpers (GetShaderInfoLog etc.) query these to size buffers.
			if (pname === 0x8B84 /* INFO_LOG_LENGTH */) {
				const len = gl().getShaderInfoLog(sh)?.length ?? 0;
				writeInt32(paramsPtr, len === 0 ? 0 : len + 1);
				return;
			}
			if (pname === 0x8B88 /* SHADER_SOURCE_LENGTH */) {
				const len = gl().getShaderSource(sh)?.length ?? 0;
				writeInt32(paramsPtr, len === 0 ? 0 : len + 1);
				return;
			}
			const v = gl().getShaderParameter(sh, pname);
			writeInt32(paramsPtr, typeof v === "boolean" ? (v ? 1 : 0) : (v | 0));
		}

		public static glGetShaderInfoLog(shader: number, bufSize: number, lengthPtr: number, infoLogPtr: number): void {
			const sh = tables().shaders[shader];
			const log = sh ? (gl().getShaderInfoLog(sh) ?? "") : "";
			writeCString(infoLogPtr, bufSize, log, lengthPtr);
		}

		public static glCreateProgram(): number {
			const ctx = gl();
			const t = tables();
			const p = ctx.createProgram() as GlAny;
			if (!p) return 0;
			const id = getNewId(t.programs);
			p.name = id;
			t.programs[id] = p;
			return id;
		}

		public static glDeleteProgram(program: number): void {
			const t = tables();
			const p = t.programs[program];
			if (p) {
				gl().deleteProgram(p);
				t.programs[program] = null;
			}
		}

		public static glAttachShader(program: number, shader: number): void {
			const t = tables();
			const p = t.programs[program];
			const sh = t.shaders[shader];
			if (p && sh) gl().attachShader(p, sh);
		}

		public static glDetachShader(program: number, shader: number): void {
			const t = tables();
			const p = t.programs[program];
			const sh = t.shaders[shader];
			if (p && sh) gl().detachShader(p, sh);
		}

		public static glLinkProgram(program: number): void {
			const p = tables().programs[program];
			if (p) gl().linkProgram(p);
		}

		public static glGetProgramiv(program: number, pname: number, paramsPtr: number): void {
			const p = tables().programs[program];
			if (!p) { writeInt32(paramsPtr, 0); return; }
			// WebGL2 dropped the *_LENGTH pnames (strings are returned directly); compute them.
			// Silk.NET's string-returning helpers (GetActiveUniform etc.) query these to size buffers.
			if (pname === 0x8B84 /* INFO_LOG_LENGTH */) {
				const len = gl().getProgramInfoLog(p)?.length ?? 0;
				writeInt32(paramsPtr, len === 0 ? 0 : len + 1);
				return;
			}
			if (pname === 0x8B87 /* ACTIVE_UNIFORM_MAX_LENGTH */ || pname === 0x8B8A /* ACTIVE_ATTRIBUTE_MAX_LENGTH */) {
				const ctx = gl();
				const isUniform = pname === 0x8B87;
				const count = ctx.getProgramParameter(p, isUniform ? ctx.ACTIVE_UNIFORMS : ctx.ACTIVE_ATTRIBUTES) as number;
				let max = 0;
				for (let i = 0; i < count; i++) {
					const info = isUniform ? ctx.getActiveUniform(p, i) : ctx.getActiveAttrib(p, i);
					if (info) max = Math.max(max, info.name.length);
				}
				writeInt32(paramsPtr, max === 0 ? 0 : max + 1);
				return;
			}
			if (pname === 0x8C76 /* TRANSFORM_FEEDBACK_VARYING_MAX_LENGTH */) {
				const ctx = gl();
				const count = ctx.getProgramParameter(p, ctx.TRANSFORM_FEEDBACK_VARYINGS) as number;
				let max = 0;
				for (let i = 0; i < count; i++) {
					const info = ctx.getTransformFeedbackVarying(p, i);
					if (info) max = Math.max(max, info.name.length);
				}
				writeInt32(paramsPtr, max === 0 ? 0 : max + 1);
				return;
			}
			const v = gl().getProgramParameter(p, pname);
			writeInt32(paramsPtr, typeof v === "boolean" ? (v ? 1 : 0) : (v | 0));
		}

		public static glGetProgramInfoLog(program: number, bufSize: number, lengthPtr: number, infoLogPtr: number): void {
			const p = tables().programs[program];
			const log = p ? (gl().getProgramInfoLog(p) ?? "") : "";
			writeCString(infoLogPtr, bufSize, log, lengthPtr);
		}

		public static glUseProgram(program: number): void {
			gl().useProgram(program === 0 ? null : tables().programs[program]);
		}

		public static glGetUniformLocation(program: number, namePtr: number): number {
			const t = tables();
			const p = t.programs[program];
			if (!p) return -1;
			const name = readCString(namePtr);
			const loc = gl().getUniformLocation(p, name);
			if (!loc) return -1;
			const id = getNewId(t.uniforms);
			t.uniforms[id] = loc;
			return id;
		}

		public static glUniform1i(location: number, v0: number): void {
			const t = tables();
			const loc = location < 0 ? null : t.uniforms[location];
			if (loc) gl().uniform1i(loc, v0);
		}

		public static glUniform1f(location: number, v0: number): void {
			const t = tables();
			const loc = location < 0 ? null : t.uniforms[location];
			if (loc) gl().uniform1f(loc, v0);
		}

		public static glUniform4f(location: number, v0: number, v1: number, v2: number, v3: number): void {
			const t = tables();
			const loc = location < 0 ? null : t.uniforms[location];
			if (loc) gl().uniform4f(loc, v0, v1, v2, v3);
		}

		// ------------------------------------------------------------------------------------
		// Per-fragment / framebuffer state
		// ------------------------------------------------------------------------------------

		public static glBlendFunc(sfactor: number, dfactor: number): void { gl().blendFunc(sfactor, dfactor); }
		public static glBlendFuncSeparate(srcRGB: number, dstRGB: number, srcAlpha: number, dstAlpha: number): void { gl().blendFuncSeparate(srcRGB, dstRGB, srcAlpha, dstAlpha); }
		public static glBlendEquation(mode: number): void { gl().blendEquation(mode); }
		public static glBlendEquationSeparate(modeRGB: number, modeAlpha: number): void { gl().blendEquationSeparate(modeRGB, modeAlpha); }
		public static glBlendColor(r: number, g: number, b: number, a: number): void { gl().blendColor(r, g, b, a); }

		public static glDepthFunc(func: number): void { gl().depthFunc(func); }
		public static glDepthMask(flag: number): void { gl().depthMask(flag !== 0); }
		public static glDepthRangef(n: number, f: number): void { gl().depthRange(n, f); }

		public static glStencilFunc(func: number, ref: number, mask: number): void {
			const ctx = gl();
			ctx.stencilFunc(func, ref, mask);
			recordStencilMismatch(ctx);
		}
		public static glStencilFuncSeparate(face: number, func: number, ref: number, mask: number): void {
			const ctx = gl();
			ctx.stencilFuncSeparate(face, func, ref, mask);
			recordStencilMismatch(ctx);
		}
		public static glStencilMask(mask: number): void {
			const ctx = gl();
			ctx.stencilMask(mask);
			recordStencilMismatch(ctx);
		}
		public static glStencilMaskSeparate(face: number, mask: number): void {
			const ctx = gl();
			ctx.stencilMaskSeparate(face, mask);
			recordStencilMismatch(ctx);
		}
		public static glStencilOp(fail: number, zfail: number, zpass: number): void { gl().stencilOp(fail, zfail, zpass); }
		public static glStencilOpSeparate(face: number, sfail: number, dpfail: number, dppass: number): void { gl().stencilOpSeparate(face, sfail, dpfail, dppass); }

		public static glCullFace(mode: number): void { gl().cullFace(mode); }
		public static glFrontFace(mode: number): void { gl().frontFace(mode); }
		public static glScissor(x: number, y: number, width: number, height: number): void { gl().scissor(x, y, width, height); }
		public static glPolygonOffset(factor: number, units: number): void { gl().polygonOffset(factor, units); }
		public static glColorMask(red: number, green: number, blue: number, alpha: number): void { gl().colorMask(red !== 0, green !== 0, blue !== 0, alpha !== 0); }
		public static glLineWidth(width: number): void { gl().lineWidth(width); }
		public static glHint(target: number, mode: number): void { gl().hint(target, mode); }
		public static glFinish(): void { gl().finish(); }
		public static glFlush(): void { gl().flush(); }
		public static glSampleCoverage(value: number, invert: number): void { gl().sampleCoverage(value, invert !== 0); }

		public static glIsEnabled(cap: number): number { return gl().isEnabled(cap) ? 1 : 0; }
		public static glIsBuffer(buffer: number): number {
			const obj = buffer === 0 ? null : tables().buffers[buffer];
			return obj && gl().isBuffer(obj) ? 1 : 0;
		}
		public static glIsFramebuffer(framebuffer: number): number {
			const obj = framebuffer === 0 ? null : tables().framebuffers[framebuffer];
			return obj && gl().isFramebuffer(obj) ? 1 : 0;
		}
		public static glIsProgram(program: number): number {
			const obj = program === 0 ? null : tables().programs[program];
			return obj && gl().isProgram(obj) ? 1 : 0;
		}
		public static glIsRenderbuffer(renderbuffer: number): number {
			const obj = renderbuffer === 0 ? null : tables().renderbuffers[renderbuffer];
			return obj && gl().isRenderbuffer(obj) ? 1 : 0;
		}
		public static glIsShader(shader: number): number {
			const obj = shader === 0 ? null : tables().shaders[shader];
			return obj && gl().isShader(obj) ? 1 : 0;
		}
		public static glIsTexture(texture: number): number {
			const obj = texture === 0 ? null : tables().textures[texture];
			return obj && gl().isTexture(obj) ? 1 : 0;
		}
		public static glIsVertexArray(array: number): number {
			const obj = array === 0 ? null : tables().vaos[array];
			return obj && gl().isVertexArray(obj) ? 1 : 0;
		}

		public static glGetBooleanv(pname: number, dataPtr: number): void {
			const result = gl().getParameter(pname);
			const HEAPU8 = (<GlAny>window).Module.HEAPU8 as Uint8Array;
			if (typeof result === "boolean") {
				HEAPU8[dataPtr] = result ? 1 : 0;
			} else if (typeof result === "number") {
				HEAPU8[dataPtr] = result !== 0 ? 1 : 0;
			} else if (result && typeof (result as any).length === "number") {
				for (let i = 0; i < (result as any).length; i++) {
					HEAPU8[dataPtr + i] = (result as any)[i] ? 1 : 0;
				}
			} else {
				HEAPU8[dataPtr] = 0;
			}
		}

		public static glGetFloatv(pname: number, dataPtr: number): void {
			const result = gl().getParameter(pname);
			const HEAPF32 = (<GlAny>window).Module.HEAPF32 as Float32Array;
			if (typeof result === "number") {
				HEAPF32[dataPtr >> 2] = result;
			} else if (result && typeof (result as any).length === "number") {
				for (let i = 0; i < (result as any).length; i++) {
					HEAPF32[(dataPtr >> 2) + i] = (result as any)[i];
				}
			} else {
				HEAPF32[dataPtr >> 2] = 0;
			}
		}

		// ------------------------------------------------------------------------------------
		// Uniforms (scalar / vector / matrix / introspection)
		// ------------------------------------------------------------------------------------

		private static uniformLoc(location: number): WebGLUniformLocation | null {
			return location < 0 ? null : tables().uniforms[location];
		}

		public static glUniform2i(location: number, v0: number, v1: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform2i(loc, v0, v1); }
		public static glUniform3i(location: number, v0: number, v1: number, v2: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform3i(loc, v0, v1, v2); }
		public static glUniform4i(location: number, v0: number, v1: number, v2: number, v3: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform4i(loc, v0, v1, v2, v3); }
		public static glUniform2f(location: number, v0: number, v1: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform2f(loc, v0, v1); }
		public static glUniform3f(location: number, v0: number, v1: number, v2: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform3f(loc, v0, v1, v2); }
		public static glUniform1ui(location: number, v0: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform1ui(loc, v0); }
		public static glUniform2ui(location: number, v0: number, v1: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform2ui(loc, v0, v1); }
		public static glUniform3ui(location: number, v0: number, v1: number, v2: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform3ui(loc, v0, v1, v2); }
		public static glUniform4ui(location: number, v0: number, v1: number, v2: number, v3: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform4ui(loc, v0, v1, v2, v3); }

		// Vector overloads: the JS WebGL2 API accepts typed array views with offset/length.
		// We slice() to a fresh array so wasm heap re-growth (which can detach views) doesn't
		// cause the runtime to throw during the call.
		public static glUniform1iv(location: number, count: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform1iv(loc, new Int32Array((<GlAny>window).Module.HEAP32.buffer, valuePtr, count).slice()); }
		public static glUniform2iv(location: number, count: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform2iv(loc, new Int32Array((<GlAny>window).Module.HEAP32.buffer, valuePtr, count * 2).slice()); }
		public static glUniform3iv(location: number, count: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform3iv(loc, new Int32Array((<GlAny>window).Module.HEAP32.buffer, valuePtr, count * 3).slice()); }
		public static glUniform4iv(location: number, count: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform4iv(loc, new Int32Array((<GlAny>window).Module.HEAP32.buffer, valuePtr, count * 4).slice()); }
		public static glUniform1fv(location: number, count: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform1fv(loc, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, valuePtr, count).slice()); }
		public static glUniform2fv(location: number, count: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform2fv(loc, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, valuePtr, count * 2).slice()); }
		public static glUniform3fv(location: number, count: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform3fv(loc, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, valuePtr, count * 3).slice()); }
		public static glUniform4fv(location: number, count: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform4fv(loc, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, valuePtr, count * 4).slice()); }
		public static glUniform1uiv(location: number, count: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform1uiv(loc, new Uint32Array((<GlAny>window).Module.HEAPU32.buffer, valuePtr, count).slice()); }
		public static glUniform2uiv(location: number, count: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform2uiv(loc, new Uint32Array((<GlAny>window).Module.HEAPU32.buffer, valuePtr, count * 2).slice()); }
		public static glUniform3uiv(location: number, count: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform3uiv(loc, new Uint32Array((<GlAny>window).Module.HEAPU32.buffer, valuePtr, count * 3).slice()); }
		public static glUniform4uiv(location: number, count: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniform4uiv(loc, new Uint32Array((<GlAny>window).Module.HEAPU32.buffer, valuePtr, count * 4).slice()); }

		public static glUniformMatrix2fv(location: number, count: number, transpose: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniformMatrix2fv(loc, transpose !== 0, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, valuePtr, count * 4).slice()); }
		public static glUniformMatrix3fv(location: number, count: number, transpose: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniformMatrix3fv(loc, transpose !== 0, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, valuePtr, count * 9).slice()); }
		public static glUniformMatrix4fv(location: number, count: number, transpose: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniformMatrix4fv(loc, transpose !== 0, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, valuePtr, count * 16).slice()); }
		public static glUniformMatrix2x3fv(location: number, count: number, transpose: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniformMatrix2x3fv(loc, transpose !== 0, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, valuePtr, count * 6).slice()); }
		public static glUniformMatrix3x2fv(location: number, count: number, transpose: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniformMatrix3x2fv(loc, transpose !== 0, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, valuePtr, count * 6).slice()); }
		public static glUniformMatrix2x4fv(location: number, count: number, transpose: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniformMatrix2x4fv(loc, transpose !== 0, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, valuePtr, count * 8).slice()); }
		public static glUniformMatrix4x2fv(location: number, count: number, transpose: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniformMatrix4x2fv(loc, transpose !== 0, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, valuePtr, count * 8).slice()); }
		public static glUniformMatrix3x4fv(location: number, count: number, transpose: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniformMatrix3x4fv(loc, transpose !== 0, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, valuePtr, count * 12).slice()); }
		public static glUniformMatrix4x3fv(location: number, count: number, transpose: number, valuePtr: number): void { const loc = WasmGLFunctions.uniformLoc(location); if (loc) gl().uniformMatrix4x3fv(loc, transpose !== 0, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, valuePtr, count * 12).slice()); }

		public static glGetUniformfv(program: number, location: number, paramsPtr: number): void {
			const p = tables().programs[program];
			const loc = WasmGLFunctions.uniformLoc(location);
			if (!p || !loc) return;
			const v = gl().getUniform(p, loc);
			const HEAPF32 = (<GlAny>window).Module.HEAPF32 as Float32Array;
			if (typeof v === "number") HEAPF32[paramsPtr >> 2] = v;
			else if (v && typeof (v as any).length === "number") {
				for (let i = 0; i < (v as any).length; i++) HEAPF32[(paramsPtr >> 2) + i] = (v as any)[i];
			}
		}
		public static glGetUniformiv(program: number, location: number, paramsPtr: number): void {
			const p = tables().programs[program];
			const loc = WasmGLFunctions.uniformLoc(location);
			if (!p || !loc) return;
			const v = gl().getUniform(p, loc);
			const HEAP32 = (<GlAny>window).Module.HEAP32 as Int32Array;
			if (typeof v === "number") HEAP32[paramsPtr >> 2] = v | 0;
			else if (typeof v === "boolean") HEAP32[paramsPtr >> 2] = v ? 1 : 0;
			else if (v && typeof (v as any).length === "number") {
				for (let i = 0; i < (v as any).length; i++) HEAP32[(paramsPtr >> 2) + i] = (v as any)[i] | 0;
			}
		}
		public static glGetUniformuiv(program: number, location: number, paramsPtr: number): void {
			const p = tables().programs[program];
			const loc = WasmGLFunctions.uniformLoc(location);
			if (!p || !loc) return;
			const v = gl().getUniform(p, loc);
			const HEAPU32 = (<GlAny>window).Module.HEAPU32 as Uint32Array;
			if (typeof v === "number") HEAPU32[paramsPtr >> 2] = v >>> 0;
			else if (v && typeof (v as any).length === "number") {
				for (let i = 0; i < (v as any).length; i++) HEAPU32[(paramsPtr >> 2) + i] = (v as any)[i] >>> 0;
			}
		}

		public static glGetActiveAttrib(program: number, index: number, bufSize: number, lengthPtr: number, sizePtr: number, typePtr: number, namePtr: number): void {
			const p = tables().programs[program];
			if (!p) return;
			const info = gl().getActiveAttrib(p, index);
			if (!info) return;
			writeInt32(sizePtr, info.size);
			writeInt32(typePtr, info.type);
			writeCString(namePtr, bufSize, info.name, lengthPtr);
		}
		public static glGetActiveUniform(program: number, index: number, bufSize: number, lengthPtr: number, sizePtr: number, typePtr: number, namePtr: number): void {
			const p = tables().programs[program];
			if (!p) return;
			const info = gl().getActiveUniform(p, index);
			if (!info) return;
			writeInt32(sizePtr, info.size);
			writeInt32(typePtr, info.type);
			writeCString(namePtr, bufSize, info.name, lengthPtr);
		}
		public static glGetAttribLocation(program: number, namePtr: number): number {
			const p = tables().programs[program];
			if (!p) return -1;
			return gl().getAttribLocation(p, readCString(namePtr));
		}
		public static glBindAttribLocation(program: number, index: number, namePtr: number): void {
			const p = tables().programs[program];
			if (!p) return;
			gl().bindAttribLocation(p, index, readCString(namePtr));
		}
		public static glValidateProgram(program: number): void {
			const p = tables().programs[program];
			if (p) gl().validateProgram(p);
		}
		public static glGetShaderSource(shader: number, bufSize: number, lengthPtr: number, sourcePtr: number): void {
			const sh = tables().shaders[shader];
			const src = sh ? (gl().getShaderSource(sh) ?? "") : "";
			writeCString(sourcePtr, bufSize, src, lengthPtr);
		}
		public static glGetFragDataLocation(program: number, namePtr: number): number {
			const p = tables().programs[program];
			if (!p) return -1;
			return gl().getFragDataLocation(p, readCString(namePtr));
		}

		public static glGetUniformBlockIndex(program: number, namePtr: number): number {
			const p = tables().programs[program];
			if (!p) return 0xFFFFFFFF | 0; // GL_INVALID_INDEX
			return gl().getUniformBlockIndex(p, readCString(namePtr));
		}
		public static glUniformBlockBinding(program: number, uniformBlockIndex: number, uniformBlockBinding: number): void {
			const p = tables().programs[program];
			if (p) gl().uniformBlockBinding(p, uniformBlockIndex, uniformBlockBinding);
		}
		public static glGetActiveUniformBlockiv(program: number, uniformBlockIndex: number, pname: number, paramsPtr: number): void {
			const p = tables().programs[program];
			if (!p) return;
			const v = gl().getActiveUniformBlockParameter(p, uniformBlockIndex, pname);
			const HEAP32 = (<GlAny>window).Module.HEAP32 as Int32Array;
			if (typeof v === "number") HEAP32[paramsPtr >> 2] = v | 0;
			else if (typeof v === "boolean") HEAP32[paramsPtr >> 2] = v ? 1 : 0;
			else if (v && typeof (v as any).length === "number") {
				for (let i = 0; i < (v as any).length; i++) HEAP32[(paramsPtr >> 2) + i] = (v as any)[i] | 0;
			}
		}
		public static glGetActiveUniformBlockName(program: number, uniformBlockIndex: number, bufSize: number, lengthPtr: number, namePtr: number): void {
			const p = tables().programs[program];
			const name = p ? (gl().getActiveUniformBlockName(p, uniformBlockIndex) ?? "") : "";
			writeCString(namePtr, bufSize, name, lengthPtr);
		}
		public static glGetUniformIndices(program: number, uniformCount: number, uniformNamesPtr: number, uniformIndicesPtr: number): void {
			const p = tables().programs[program];
			if (!p) return;
			const ptrs = readUInt32Array(uniformNamesPtr, uniformCount);
			const names: string[] = [];
			for (let i = 0; i < uniformCount; i++) names.push(readCString(ptrs[i]));
			// getUniformIndices returns Iterable<GLuint> in the .d.ts; widen to any for indexed access.
			const indices = gl().getUniformIndices(p, names) as any;
			const HEAPU32 = (<GlAny>window).Module.HEAPU32 as Uint32Array;
			for (let i = 0; i < uniformCount; i++) HEAPU32[(uniformIndicesPtr >> 2) + i] = (indices?.[i] ?? 0xFFFFFFFF) >>> 0;
		}
		public static glGetActiveUniformsiv(program: number, uniformCount: number, uniformIndicesPtr: number, pname: number, paramsPtr: number): void {
			const p = tables().programs[program];
			if (!p) return;
			const indices = readUInt32Array(uniformIndicesPtr, uniformCount);
			// getActiveUniforms returns Iterable<...>; widen to any.
			const values = gl().getActiveUniforms(p, Array.from(indices), pname) as any;
			const HEAP32 = (<GlAny>window).Module.HEAP32 as Int32Array;
			for (let i = 0; i < uniformCount; i++) {
				const v = values?.[i] ?? 0;
				HEAP32[(paramsPtr >> 2) + i] = (typeof v === "boolean" ? (v ? 1 : 0) : v) | 0;
			}
		}

		// ------------------------------------------------------------------------------------
		// Buffers (extras beyond core)
		// ------------------------------------------------------------------------------------

		public static glBufferSubData(target: number, offset: number, size: number, dataPtr: number): void {
			const HEAPU8 = (<GlAny>window).Module.HEAPU8 as Uint8Array;
			gl().bufferSubData(target, offset, HEAPU8.subarray(dataPtr, dataPtr + size));
		}
		public static glCopyBufferSubData(readTarget: number, writeTarget: number, readOffset: number, writeOffset: number, size: number): void {
			gl().copyBufferSubData(readTarget, writeTarget, readOffset, writeOffset, size);
		}
		public static glBindBufferBase(target: number, index: number, buffer: number): void {
			gl().bindBufferBase(target, index, buffer === 0 ? null : tables().buffers[buffer]);
		}
		public static glBindBufferRange(target: number, index: number, buffer: number, offset: number, size: number): void {
			gl().bindBufferRange(target, index, buffer === 0 ? null : tables().buffers[buffer], offset, size);
		}
		public static glGetBufferParameteriv(target: number, pname: number, paramsPtr: number): void {
			const v = gl().getBufferParameter(target, pname);
			writeInt32(paramsPtr, typeof v === "number" ? (v | 0) : 0);
		}

		// ------------------------------------------------------------------------------------
		// Vertex attribute set/get variants
		// ------------------------------------------------------------------------------------

		public static glVertexAttrib1f(index: number, v0: number): void { gl().vertexAttrib1f(index, v0); }
		public static glVertexAttrib2f(index: number, v0: number, v1: number): void { gl().vertexAttrib2f(index, v0, v1); }
		public static glVertexAttrib3f(index: number, v0: number, v1: number, v2: number): void { gl().vertexAttrib3f(index, v0, v1, v2); }
		public static glVertexAttrib4f(index: number, v0: number, v1: number, v2: number, v3: number): void { gl().vertexAttrib4f(index, v0, v1, v2, v3); }
		public static glVertexAttrib1fv(index: number, vPtr: number): void { gl().vertexAttrib1fv(index, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, vPtr, 1).slice()); }
		public static glVertexAttrib2fv(index: number, vPtr: number): void { gl().vertexAttrib2fv(index, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, vPtr, 2).slice()); }
		public static glVertexAttrib3fv(index: number, vPtr: number): void { gl().vertexAttrib3fv(index, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, vPtr, 3).slice()); }
		public static glVertexAttrib4fv(index: number, vPtr: number): void { gl().vertexAttrib4fv(index, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, vPtr, 4).slice()); }
		public static glVertexAttribI4i(index: number, x: number, y: number, z: number, w: number): void { gl().vertexAttribI4i(index, x, y, z, w); }
		public static glVertexAttribI4ui(index: number, x: number, y: number, z: number, w: number): void { gl().vertexAttribI4ui(index, x, y, z, w); }
		public static glVertexAttribI4iv(index: number, vPtr: number): void { gl().vertexAttribI4iv(index, new Int32Array((<GlAny>window).Module.HEAP32.buffer, vPtr, 4).slice()); }
		public static glVertexAttribI4uiv(index: number, vPtr: number): void { gl().vertexAttribI4uiv(index, new Uint32Array((<GlAny>window).Module.HEAPU32.buffer, vPtr, 4).slice()); }
		public static glVertexAttribIPointer(index: number, size: number, type: number, stride: number, pointer: number): void {
			gl().vertexAttribIPointer(index, size, type, stride, pointer);
		}
		public static glVertexAttribDivisor(index: number, divisor: number): void { gl().vertexAttribDivisor(index, divisor); }
		public static glGetVertexAttribfv(index: number, pname: number, paramsPtr: number): void {
			const v = gl().getVertexAttrib(index, pname);
			const HEAPF32 = (<GlAny>window).Module.HEAPF32 as Float32Array;
			if (typeof v === "number") HEAPF32[paramsPtr >> 2] = v;
			else if (v && typeof (v as any).length === "number") for (let i = 0; i < (v as any).length; i++) HEAPF32[(paramsPtr >> 2) + i] = (v as any)[i];
		}
		public static glGetVertexAttribiv(index: number, pname: number, paramsPtr: number): void {
			const v = gl().getVertexAttrib(index, pname);
			const HEAP32 = (<GlAny>window).Module.HEAP32 as Int32Array;
			if (typeof v === "number") HEAP32[paramsPtr >> 2] = v | 0;
			else if (typeof v === "boolean") HEAP32[paramsPtr >> 2] = v ? 1 : 0;
			else if (v && typeof (v as any).length === "number") for (let i = 0; i < (v as any).length; i++) HEAP32[(paramsPtr >> 2) + i] = (v as any)[i] | 0;
		}
		public static glGetVertexAttribIiv(index: number, pname: number, paramsPtr: number): void { WasmGLFunctions.glGetVertexAttribiv(index, pname, paramsPtr); }
		public static glGetVertexAttribIuiv(index: number, pname: number, paramsPtr: number): void {
			const v = gl().getVertexAttrib(index, pname);
			const HEAPU32 = (<GlAny>window).Module.HEAPU32 as Uint32Array;
			if (typeof v === "number") HEAPU32[paramsPtr >> 2] = v >>> 0;
			else if (v && typeof (v as any).length === "number") for (let i = 0; i < (v as any).length; i++) HEAPU32[(paramsPtr >> 2) + i] = (v as any)[i] >>> 0;
		}
		public static glGetVertexAttribPointerv(index: number, pname: number, pointerPtr: number): void {
			const v = gl().getVertexAttribOffset(index, pname);
			writeInt32(pointerPtr, v | 0);
		}

		// ------------------------------------------------------------------------------------
		// Textures (extras)
		// ------------------------------------------------------------------------------------

		public static glTexParameterf(target: number, pname: number, param: number): void { gl().texParameterf(target, pname, param); }
		public static glTexParameterfv(target: number, pname: number, paramsPtr: number): void {
			const v = (<GlAny>window).Module.HEAPF32[paramsPtr >> 2];
			gl().texParameterf(target, pname, v);
		}
		public static glTexParameteriv(target: number, pname: number, paramsPtr: number): void {
			const v = (<GlAny>window).Module.HEAP32[paramsPtr >> 2];
			gl().texParameteri(target, pname, v);
		}
		public static glGenerateMipmap(target: number): void { gl().generateMipmap(target); }
		public static glCopyTexImage2D(target: number, level: number, internalformat: number, x: number, y: number, width: number, height: number, border: number): void {
			gl().copyTexImage2D(target, level, internalformat, x, y, width, height, border);
		}
		public static glCopyTexSubImage2D(target: number, level: number, xoffset: number, yoffset: number, x: number, y: number, width: number, height: number): void {
			gl().copyTexSubImage2D(target, level, xoffset, yoffset, x, y, width, height);
		}
		public static glTexStorage2D(target: number, levels: number, internalformat: number, width: number, height: number): void {
			gl().texStorage2D(target, levels, internalformat, width, height);
		}
		public static glTexStorage3D(target: number, levels: number, internalformat: number, width: number, height: number, depth: number): void {
			gl().texStorage3D(target, levels, internalformat, width, height, depth);
		}
		public static glCompressedTexImage2D(target: number, level: number, internalformat: number, width: number, height: number, border: number, imageSize: number, dataPtr: number): void {
			const HEAPU8 = (<GlAny>window).Module.HEAPU8 as Uint8Array;
			gl().compressedTexImage2D(target, level, internalformat, width, height, border, HEAPU8.subarray(dataPtr, dataPtr + imageSize));
		}
		public static glGetTexParameterfv(target: number, pname: number, paramsPtr: number): void {
			const v = gl().getTexParameter(target, pname);
			(<GlAny>window).Module.HEAPF32[paramsPtr >> 2] = typeof v === "number" ? v : 0;
		}
		public static glGetTexParameteriv(target: number, pname: number, paramsPtr: number): void {
			const v = gl().getTexParameter(target, pname);
			writeInt32(paramsPtr, typeof v === "number" ? (v | 0) : (v ? 1 : 0));
		}
		public static glGetInternalformativ(target: number, internalformat: number, pname: number, bufSize: number, paramsPtr: number): void {
			// WebGL2 spec returns Int32Array but lib.dom.d.ts types it as Iterable<number>; cast through any.
			const v = gl().getInternalformatParameter(target, internalformat, pname) as any;
			if (!v) return;
			const HEAP32 = (<GlAny>window).Module.HEAP32 as Int32Array;
			const len = Math.min(bufSize, v.length);
			for (let i = 0; i < len; i++) HEAP32[(paramsPtr >> 2) + i] = v[i] | 0;
		}

		// ------------------------------------------------------------------------------------
		// Framebuffers (extras)
		// ------------------------------------------------------------------------------------

		public static glDrawBuffers(n: number, bufsPtr: number): void {
			const HEAP32 = (<GlAny>window).Module.HEAP32 as Int32Array;
			const arr: number[] = [];
			for (let i = 0; i < n; i++) arr.push(HEAP32[(bufsPtr >> 2) + i]);
			gl().drawBuffers(arr);
		}
		public static glClearBufferiv(buffer: number, drawbuffer: number, valuePtr: number): void {
			gl().clearBufferiv(buffer, drawbuffer, new Int32Array((<GlAny>window).Module.HEAP32.buffer, valuePtr, 4).slice());
		}
		public static glClearBufferuiv(buffer: number, drawbuffer: number, valuePtr: number): void {
			gl().clearBufferuiv(buffer, drawbuffer, new Uint32Array((<GlAny>window).Module.HEAPU32.buffer, valuePtr, 4).slice());
		}
		public static glClearBufferfv(buffer: number, drawbuffer: number, valuePtr: number): void {
			gl().clearBufferfv(buffer, drawbuffer, new Float32Array((<GlAny>window).Module.HEAPF32.buffer, valuePtr, 4).slice());
		}
		public static glClearBufferfi(buffer: number, drawbuffer: number, depth: number, stencil: number): void {
			gl().clearBufferfi(buffer, drawbuffer, depth, stencil);
		}
		public static glRenderbufferStorageMultisample(target: number, samples: number, internalformat: number, width: number, height: number): void {
			gl().renderbufferStorageMultisample(target, samples, internalformat, width, height);
		}
		public static glFramebufferTextureLayer(target: number, attachment: number, texture: number, level: number, layer: number): void {
			gl().framebufferTextureLayer(target, attachment, texture === 0 ? null : tables().textures[texture], level, layer);
		}
		public static glInvalidateFramebuffer(target: number, numAttachments: number, attachmentsPtr: number): void {
			const HEAP32 = (<GlAny>window).Module.HEAP32 as Int32Array;
			const arr: number[] = [];
			for (let i = 0; i < numAttachments; i++) arr.push(HEAP32[(attachmentsPtr >> 2) + i]);
			gl().invalidateFramebuffer(target, arr);
		}
		public static glInvalidateSubFramebuffer(target: number, numAttachments: number, attachmentsPtr: number, x: number, y: number, width: number, height: number): void {
			const HEAP32 = (<GlAny>window).Module.HEAP32 as Int32Array;
			const arr: number[] = [];
			for (let i = 0; i < numAttachments; i++) arr.push(HEAP32[(attachmentsPtr >> 2) + i]);
			gl().invalidateSubFramebuffer(target, arr, x, y, width, height);
		}
		public static glGetFramebufferAttachmentParameteriv(target: number, attachment: number, pname: number, paramsPtr: number): void {
			const v = gl().getFramebufferAttachmentParameter(target, attachment, pname);
			writeInt32(paramsPtr, typeof v === "number" ? (v | 0) : (v && typeof (v as any).name === "number" ? (v as any).name : 0));
		}
		public static glGetRenderbufferParameteriv(target: number, pname: number, paramsPtr: number): void {
			const v = gl().getRenderbufferParameter(target, pname);
			writeInt32(paramsPtr, typeof v === "number" ? (v | 0) : 0);
		}
		public static glClearDepthf(d: number): void { gl().clearDepth(d); }
		public static glClearStencil(s: number): void { gl().clearStencil(s); }

		// ------------------------------------------------------------------------------------
		// Drawing variants
		// ------------------------------------------------------------------------------------

		public static glDrawArraysInstanced(mode: number, first: number, count: number, instanceCount: number): void {
			const ctx = gl();
			warnIfStencilMismatch(ctx);
			ctx.drawArraysInstanced(mode, first, count, instanceCount);
		}
		public static glDrawElementsInstanced(mode: number, count: number, type: number, indicesPtr: number, instanceCount: number): void {
			const ctx = gl();
			warnIfStencilMismatch(ctx);
			ctx.drawElementsInstanced(mode, count, type, indicesPtr, instanceCount);
		}
		public static glDrawRangeElements(mode: number, start: number, end: number, count: number, type: number, indicesPtr: number): void {
			const ctx = gl();
			warnIfStencilMismatch(ctx);
			ctx.drawRangeElements(mode, start, end, count, type, indicesPtr);
		}

		// ------------------------------------------------------------------------------------
		// Samplers
		// ------------------------------------------------------------------------------------

		public static glGenSamplers(count: number, samplersPtr: number): void {
			const ctx = gl(); const t = tables();
			for (let i = 0; i < count; i++) {
				const s = ctx.createSampler() as GlAny;
				const id = getNewId(t.samplers);
				if (s) s.name = id;
				t.samplers[id] = s;
				writeUInt32(samplersPtr + i * 4, id);
			}
		}
		public static glDeleteSamplers(count: number, samplersPtr: number): void {
			const ctx = gl(); const t = tables();
			const ids = readUInt32Array(samplersPtr, count);
			for (let i = 0; i < count; i++) {
				const s = t.samplers[ids[i]];
				if (s) { ctx.deleteSampler(s); t.samplers[ids[i]] = null; }
			}
		}
		public static glIsSampler(sampler: number): number {
			const obj = sampler === 0 ? null : tables().samplers[sampler];
			return obj && gl().isSampler(obj) ? 1 : 0;
		}
		public static glBindSampler(unit: number, sampler: number): void {
			gl().bindSampler(unit, sampler === 0 ? null : tables().samplers[sampler]);
		}
		public static glSamplerParameteri(sampler: number, pname: number, param: number): void {
			const s = tables().samplers[sampler];
			if (s) gl().samplerParameteri(s, pname, param);
		}
		public static glSamplerParameterf(sampler: number, pname: number, param: number): void {
			const s = tables().samplers[sampler];
			if (s) gl().samplerParameterf(s, pname, param);
		}
		public static glSamplerParameteriv(sampler: number, pname: number, paramsPtr: number): void {
			const s = tables().samplers[sampler];
			if (!s) return;
			const v = (<GlAny>window).Module.HEAP32[paramsPtr >> 2];
			gl().samplerParameteri(s, pname, v);
		}
		public static glSamplerParameterfv(sampler: number, pname: number, paramsPtr: number): void {
			const s = tables().samplers[sampler];
			if (!s) return;
			const v = (<GlAny>window).Module.HEAPF32[paramsPtr >> 2];
			gl().samplerParameterf(s, pname, v);
		}
		public static glGetSamplerParameteriv(sampler: number, pname: number, paramsPtr: number): void {
			const s = tables().samplers[sampler];
			if (!s) return;
			const v = gl().getSamplerParameter(s, pname);
			writeInt32(paramsPtr, typeof v === "number" ? (v | 0) : 0);
		}
		public static glGetSamplerParameterfv(sampler: number, pname: number, paramsPtr: number): void {
			const s = tables().samplers[sampler];
			if (!s) return;
			const v = gl().getSamplerParameter(s, pname);
			(<GlAny>window).Module.HEAPF32[paramsPtr >> 2] = typeof v === "number" ? v : 0;
		}

		// ------------------------------------------------------------------------------------
		// Queries
		// ------------------------------------------------------------------------------------

		public static glGenQueries(n: number, idsPtr: number): void {
			const ctx = gl(); const t = tables();
			for (let i = 0; i < n; i++) {
				const q = ctx.createQuery() as GlAny;
				const id = getNewId(t.queries);
				if (q) q.name = id;
				t.queries[id] = q;
				writeUInt32(idsPtr + i * 4, id);
			}
		}
		public static glDeleteQueries(n: number, idsPtr: number): void {
			const ctx = gl(); const t = tables();
			const ids = readUInt32Array(idsPtr, n);
			for (let i = 0; i < n; i++) {
				const q = t.queries[ids[i]];
				if (q) { ctx.deleteQuery(q); t.queries[ids[i]] = null; }
			}
		}
		public static glIsQuery(id: number): number {
			const obj = id === 0 ? null : tables().queries[id];
			return obj && gl().isQuery(obj) ? 1 : 0;
		}
		public static glBeginQuery(target: number, id: number): void {
			const q = tables().queries[id];
			if (q) gl().beginQuery(target, q);
		}
		public static glEndQuery(target: number): void { gl().endQuery(target); }
		public static glGetQueryiv(target: number, pname: number, paramsPtr: number): void {
			const v = gl().getQuery(target, pname);
			writeInt32(paramsPtr, v && typeof (v as any).name === "number" ? (v as any).name : 0);
		}
		public static glGetQueryObjectuiv(id: number, pname: number, paramsPtr: number): void {
			const q = tables().queries[id];
			if (!q) return;
			const v = gl().getQueryParameter(q, pname);
			writeUInt32(paramsPtr, typeof v === "number" ? (v >>> 0) : (v ? 1 : 0));
		}

		// ------------------------------------------------------------------------------------
		// Sync objects
		// ------------------------------------------------------------------------------------

		public static glFenceSync(condition: number, flags: number): number {
			const ctx = gl(); const t = tables();
			const s = ctx.fenceSync(condition, flags) as GlAny;
			if (!s) return 0;
			const id = getNewId(t.syncs);
			(s as GlAny).name = id;
			t.syncs[id] = s;
			return id;
		}
		public static glIsSync(sync: number): number {
			const obj = sync === 0 ? null : tables().syncs[sync];
			return obj && gl().isSync(obj) ? 1 : 0;
		}
		public static glDeleteSync(sync: number): void {
			const t = tables();
			const s = t.syncs[sync];
			if (s) { gl().deleteSync(s); t.syncs[sync] = null; }
		}
		public static glClientWaitSync(sync: number, flags: number, timeout: number): number {
			const s = tables().syncs[sync];
			return s ? gl().clientWaitSync(s, flags, timeout) : 0x911A; // GL_WAIT_FAILED
		}
		public static glWaitSync(sync: number, flags: number, timeout: number): void {
			const s = tables().syncs[sync];
			if (s) gl().waitSync(s, flags, timeout);
		}
		public static glGetSynciv(sync: number, pname: number, bufSize: number, lengthPtr: number, valuesPtr: number): void {
			const s = tables().syncs[sync];
			if (!s || bufSize <= 0) return;
			const v = gl().getSyncParameter(s, pname);
			writeInt32(valuesPtr, typeof v === "number" ? (v | 0) : 0);
			if (lengthPtr !== 0) writeInt32(lengthPtr, 1);
		}

		// ------------------------------------------------------------------------------------
		// Transform feedback
		// ------------------------------------------------------------------------------------

		public static glGenTransformFeedbacks(n: number, idsPtr: number): void {
			const ctx = gl(); const t = tables();
			for (let i = 0; i < n; i++) {
				const tf = ctx.createTransformFeedback() as GlAny;
				const id = getNewId(t.transformFeedbacks);
				if (tf) tf.name = id;
				t.transformFeedbacks[id] = tf;
				writeUInt32(idsPtr + i * 4, id);
			}
		}
		public static glDeleteTransformFeedbacks(n: number, idsPtr: number): void {
			const ctx = gl(); const t = tables();
			const ids = readUInt32Array(idsPtr, n);
			for (let i = 0; i < n; i++) {
				const tf = t.transformFeedbacks[ids[i]];
				if (tf) { ctx.deleteTransformFeedback(tf); t.transformFeedbacks[ids[i]] = null; }
			}
		}
		public static glIsTransformFeedback(id: number): number {
			const obj = id === 0 ? null : tables().transformFeedbacks[id];
			return obj && gl().isTransformFeedback(obj) ? 1 : 0;
		}
		public static glBindTransformFeedback(target: number, id: number): void {
			gl().bindTransformFeedback(target, id === 0 ? null : tables().transformFeedbacks[id]);
		}
		public static glBeginTransformFeedback(primitiveMode: number): void { gl().beginTransformFeedback(primitiveMode); }
		public static glEndTransformFeedback(): void { gl().endTransformFeedback(); }
		public static glPauseTransformFeedback(): void { gl().pauseTransformFeedback(); }
		public static glResumeTransformFeedback(): void { gl().resumeTransformFeedback(); }
		public static glTransformFeedbackVaryings(program: number, count: number, varyingsPtr: number, bufferMode: number): void {
			const p = tables().programs[program];
			if (!p) return;
			const ptrs = readUInt32Array(varyingsPtr, count);
			const names: string[] = [];
			for (let i = 0; i < count; i++) names.push(readCString(ptrs[i]));
			gl().transformFeedbackVaryings(p, names, bufferMode);
		}
		public static glGetTransformFeedbackVarying(program: number, index: number, bufSize: number, lengthPtr: number, sizePtr: number, typePtr: number, namePtr: number): void {
			const p = tables().programs[program];
			if (!p) return;
			const info = gl().getTransformFeedbackVarying(p, index);
			if (!info) return;
			writeInt32(sizePtr, info.size);
			writeInt32(typePtr, info.type);
			writeCString(namePtr, bufSize, info.name, lengthPtr);
		}

		// ------------------------------------------------------------------------------------
		// Misc: glGetAttachedShaders, glGetShaderPrecisionFormat, indexed queries, glGetStringi
		// ------------------------------------------------------------------------------------

		public static glGetAttachedShaders(program: number, maxCount: number, countPtr: number, shadersPtr: number): void {
			const p = tables().programs[program];
			if (!p) { if (countPtr) writeInt32(countPtr, 0); return; }
			const shaders = gl().getAttachedShaders(p) ?? [];
			const t = tables();
			const n = Math.min(maxCount, shaders.length);
			if (countPtr) writeInt32(countPtr, n);
			// Reverse-map WebGLShader objects to ids using the shaders table.
			for (let i = 0; i < n; i++) {
				let id = 0;
				const sh = shaders[i] as any;
				if (sh && typeof sh.name === "number") {
					id = sh.name;
				} else {
					for (let j = 1; j < t.shaders.length; j++) {
						if (t.shaders[j] === sh) { id = j; break; }
					}
				}
				writeUInt32(shadersPtr + i * 4, id);
			}
		}

		public static glGetShaderPrecisionFormat(shadertype: number, precisiontype: number, rangePtr: number, precisionPtr: number): void {
			const fmt = gl().getShaderPrecisionFormat(shadertype, precisiontype);
			if (!fmt) {
				writeInt32(rangePtr, 0); writeInt32(rangePtr + 4, 0); writeInt32(precisionPtr, 0);
				return;
			}
			writeInt32(rangePtr, fmt.rangeMin | 0);
			writeInt32(rangePtr + 4, fmt.rangeMax | 0);
			writeInt32(precisionPtr, fmt.precision | 0);
		}

		public static glGetIntegeri_v(target: number, index: number, dataPtr: number): void {
			const v = gl().getIndexedParameter(target, index);
			writeInt32(dataPtr, typeof v === "number" ? (v | 0) : 0);
		}

		public static glGetInteger64v(pname: number, dataPtr: number): void {
			const v = gl().getParameter(pname);
			// 64-bit write: low and high 32-bit halves. JavaScript Numbers can represent up to 2^53.
			const n = typeof v === "number" ? v : 0;
			const HEAP32 = (<GlAny>window).Module.HEAP32 as Int32Array;
			HEAP32[dataPtr >> 2] = n | 0;
			HEAP32[(dataPtr >> 2) + 1] = Math.floor(n / 0x100000000) | 0;
		}

		public static glGetInteger64i_v(target: number, index: number, dataPtr: number): void {
			const v = gl().getIndexedParameter(target, index);
			const n = typeof v === "number" ? v : 0;
			const HEAP32 = (<GlAny>window).Module.HEAP32 as Int32Array;
			HEAP32[dataPtr >> 2] = n | 0;
			HEAP32[(dataPtr >> 2) + 1] = Math.floor(n / 0x100000000) | 0;
		}

		public static glGetBufferParameteri64v(target: number, pname: number, paramsPtr: number): void {
			const v = gl().getBufferParameter(target, pname);
			const n = typeof v === "number" ? v : 0;
			const HEAP32 = (<GlAny>window).Module.HEAP32 as Int32Array;
			HEAP32[paramsPtr >> 2] = n | 0;
			HEAP32[(paramsPtr >> 2) + 1] = Math.floor(n / 0x100000000) | 0;
		}

		public static glGetStringi(name: number, index: number): number {
			// WebGL2 exposes indexed strings only for GL_EXTENSIONS (via getSupportedExtensions).
			const key = name * 0x10000 + index;
			const cached = stringCache[key];
			if (cached) return cached;
			let value = "";
			if (name === 0x1F03 /* GL_EXTENSIONS */) {
				const exts = gl().getSupportedExtensions() ?? [];
				if (index < exts.length) value = exts[index];
			}
			const ptr = allocCString(value);
			stringCache[key] = ptr;
			return ptr;
		}

		// ------------------------------------------------------------------------------------
		// Large-arity shims (>8 args, routed through uno_gl_shim.c)
		// ------------------------------------------------------------------------------------

		public static glTexSubImage2D(target: number, level: number, xoffset: number, yoffset: number, width: number, height: number, format: number, type: number, pixelsPtr: number): void {
			const ctx = gl();
			if (pixelsPtr === 0) {
				// WebGL2 still allows null for some cases (PBO path); pass undefined to match.
				(ctx as any).texSubImage2D(target, level, xoffset, yoffset, width, height, format, type, null);
				return;
			}
			const HEAPU8 = (<GlAny>window).Module.HEAPU8 as Uint8Array;
			const byteCount = width * height * 4;
			ctx.texSubImage2D(target, level, xoffset, yoffset, width, height, format, type, HEAPU8.subarray(pixelsPtr, pixelsPtr + byteCount));
		}

		public static glTexImage3D(target: number, level: number, internalformat: number, width: number, height: number, depth: number, border: number, format: number, type: number, pixelsPtr: number): void {
			const ctx = gl();
			if (type === 0x1401 /* GL_UNSIGNED_BYTE */) {
				if (internalformat === 0x1907 /* GL_RGB */) internalformat = 0x8051 /* GL_RGB8 */;
				else if (internalformat === 0x1908 /* GL_RGBA */) internalformat = 0x8058 /* GL_RGBA8 */;
			}
			if (pixelsPtr === 0) {
				ctx.texImage3D(target, level, internalformat, width, height, depth, border, format, type, null);
			} else {
				const HEAPU8 = (<GlAny>window).Module.HEAPU8 as Uint8Array;
				const byteCount = width * height * depth * 4;
				ctx.texImage3D(target, level, internalformat, width, height, depth, border, format, type, HEAPU8.subarray(pixelsPtr, pixelsPtr + byteCount));
			}
		}

		public static glTexSubImage3D(target: number, level: number, xoffset: number, yoffset: number, zoffset: number, width: number, height: number, depth: number, format: number, type: number, pixelsPtr: number): void {
			const ctx = gl();
			if (pixelsPtr === 0) {
				(ctx as any).texSubImage3D(target, level, xoffset, yoffset, zoffset, width, height, depth, format, type, null);
				return;
			}
			const HEAPU8 = (<GlAny>window).Module.HEAPU8 as Uint8Array;
			const byteCount = width * height * depth * 4;
			ctx.texSubImage3D(target, level, xoffset, yoffset, zoffset, width, height, depth, format, type, HEAPU8.subarray(pixelsPtr, pixelsPtr + byteCount));
		}

		public static glCopyTexSubImage3D(target: number, level: number, xoffset: number, yoffset: number, zoffset: number, x: number, y: number, width: number, height: number): void {
			gl().copyTexSubImage3D(target, level, xoffset, yoffset, zoffset, x, y, width, height);
		}

		public static glCompressedTexImage3D(target: number, level: number, internalformat: number, width: number, height: number, depth: number, border: number, imageSize: number, dataPtr: number): void {
			const HEAPU8 = (<GlAny>window).Module.HEAPU8 as Uint8Array;
			gl().compressedTexImage3D(target, level, internalformat, width, height, depth, border, HEAPU8.subarray(dataPtr, dataPtr + imageSize));
		}

		public static glCompressedTexSubImage2D(target: number, level: number, xoffset: number, yoffset: number, width: number, height: number, format: number, imageSize: number, dataPtr: number): void {
			const HEAPU8 = (<GlAny>window).Module.HEAPU8 as Uint8Array;
			gl().compressedTexSubImage2D(target, level, xoffset, yoffset, width, height, format, HEAPU8.subarray(dataPtr, dataPtr + imageSize));
		}

		public static glCompressedTexSubImage3D(target: number, level: number, xoffset: number, yoffset: number, zoffset: number, width: number, height: number, depth: number, format: number, imageSize: number, dataPtr: number): void {
			const HEAPU8 = (<GlAny>window).Module.HEAPU8 as Uint8Array;
			gl().compressedTexSubImage3D(target, level, xoffset, yoffset, zoffset, width, height, depth, format, HEAPU8.subarray(dataPtr, dataPtr + imageSize));
		}

		public static glBlitFramebuffer(srcX0: number, srcY0: number, srcX1: number, srcY1: number, dstX0: number, dstY0: number, dstX1: number, dstY1: number, mask: number, filter: number): void {
			gl().blitFramebuffer(srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);
		}
	}
}
