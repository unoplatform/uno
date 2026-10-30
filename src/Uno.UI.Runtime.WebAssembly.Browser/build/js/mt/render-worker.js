let glReadyResolve = null;
let glReadyReject = null;

let offscreenCanvas = null;

const gl = globalThis.getDotnetRuntime(0).Module.GL;

const glReadyPromise = new Promise((resolve, reject) => {
    glReadyResolve = resolve;
    glReadyReject = reject;
});

self.addEventListener("message", (e) => {
    if (!e.data) {
        return;
    }

    if (e.data.type === "uno-setup-gl") {
        setupGL(e.data);
    }
});

export function glMakeCurrent(handle) {
    gl.makeContextCurrent(handle);
}

export function glReady() {
    return glReadyPromise;
}

function setupGL(data) {
    try {
        offscreenCanvas = data.canvas;

        if (!gl) {
            throw new Error("Emscripten GL is not available");
        }

        const attrs = {
            alpha: 1, depth: 1, stencil: 8, antialias: 1,
            premultipliedAlpha: 1, preserveDrawingBuffer: 0,
            preferLowPowerToHighPerformance: 0, failIfMajorPerformanceCaveat: 0,
            majorVersion: 2, minorVersion: 0,
            enableExtensionsByDefault: 1, explicitSwapControl: 0,
            renderViaOffscreenBackBuffer: 0,
        };

        let glCtx = gl.createContext(offscreenCanvas, attrs);

        // Fallback WebGL 1.0
        if (!glCtx) {
            attrs.majorVersion = 1;
            attrs.minorVersion = 0;
            glCtx = gl.createContext(offscreenCanvas, attrs);
        }

        if (!glCtx || glCtx < 0) {
            throw new Error("Failed to create WebGL context: " + glCtx);
        }

        gl.makeContextCurrent(glCtx);

        const ctx = gl.currentContext && gl.currentContext.GLctx;

        if (!ctx) {
            throw new Error("Failed to get current WebGL context");
        }

        fixStridedUploads(ctx);

        glReadyResolve({
            glContextHandle: glCtx,
            fboId: ctx.getParameter(ctx.FRAMEBUFFER_BINDING) || 0,
            stencil: ctx.getParameter(ctx.STENCIL_BITS),
            samples: 0,
            depth: ctx.getParameter(ctx.DEPTH_BITS),
        });
    } catch (err) {
        glReadyReject(err);
    }
}

// Worker-side copy of EmscriptenWebGL.fixStridedUploads: the context is created here, out of reach
// of the main bundle, and Skia's strided glyph atlas uploads are rejected without it.
function fixStridedUploads(gl) {
    if (gl.__unoStridedUploadsFixed || typeof gl.UNPACK_ROW_LENGTH !== "number") {
        return;
    }
    gl.__unoStridedUploadsFixed = true;

    const unpack = { alignment: 4, rowLength: 0, skipRows: 0, skipPixels: 0 };
    const pixelStorei = gl.pixelStorei;
    gl.pixelStorei = function (pname, param) {
        switch (pname) {
            case gl.UNPACK_ALIGNMENT: unpack.alignment = param; break;
            case gl.UNPACK_ROW_LENGTH: unpack.rowLength = param; break;
            case gl.UNPACK_SKIP_ROWS: unpack.skipRows = param; break;
            case gl.UNPACK_SKIP_PIXELS: unpack.skipPixels = param; break;
        }
        return pixelStorei.call(gl, pname, param);
    };

    const isStrided = () => unpack.rowLength !== 0 || unpack.skipRows !== 0 || unpack.skipPixels !== 0;

    const resize = (width, height, format, type, pixels) => {
        if (!ArrayBuffer.isView(pixels)) {
            return pixels;
        }

        const bpp = bytesPerPixel(gl, format, type);
        if (!bpp || width <= 0 || height <= 0) {
            return pixels;
        }

        const alignment = unpack.alignment;
        const rowBytes = Math.ceil(((unpack.rowLength || width) * bpp) / alignment) * alignment;
        const required = (unpack.skipRows + height - 1) * rowBytes + (unpack.skipPixels + width) * bpp;
        if (pixels.byteLength >= required || pixels.byteOffset + required > pixels.buffer.byteLength) {
            return pixels;
        }

        const elementSize = pixels.BYTES_PER_ELEMENT || 1;
        const length = Math.min(Math.ceil(required / elementSize), Math.floor((pixels.buffer.byteLength - pixels.byteOffset) / elementSize));
        return new pixels.constructor(pixels.buffer, pixels.byteOffset, length);
    };

    const texImage2D = gl.texImage2D;
    gl.texImage2D = function () {
        if (arguments.length !== 9 || !isStrided()) {
            return texImage2D.apply(gl, arguments);
        }
        const args = Array.prototype.slice.call(arguments);
        args[8] = resize(args[3], args[4], args[6], args[7], args[8]);
        return texImage2D.apply(gl, args);
    };

    const texSubImage2D = gl.texSubImage2D;
    gl.texSubImage2D = function () {
        if (arguments.length !== 9 || !isStrided()) {
            return texSubImage2D.apply(gl, arguments);
        }
        const args = Array.prototype.slice.call(arguments);
        args[8] = resize(args[4], args[5], args[6], args[7], args[8]);
        return texSubImage2D.apply(gl, args);
    };
}

function bytesPerPixel(gl, format, type) {
    switch (type) {
        case gl.UNSIGNED_SHORT_5_6_5:
        case gl.UNSIGNED_SHORT_4_4_4_4:
        case gl.UNSIGNED_SHORT_5_5_5_1:
            return 2;
        case gl.UNSIGNED_INT_2_10_10_10_REV:
        case gl.UNSIGNED_INT_10F_11F_11F_REV:
        case gl.UNSIGNED_INT_5_9_9_9_REV:
        case gl.UNSIGNED_INT_24_8:
            return 4;
    }

    let componentSize;
    switch (type) {
        case gl.UNSIGNED_BYTE:
        case gl.BYTE:
            componentSize = 1; break;
        case gl.UNSIGNED_SHORT:
        case gl.SHORT:
        case gl.HALF_FLOAT:
            componentSize = 2; break;
        case gl.UNSIGNED_INT:
        case gl.INT:
        case gl.FLOAT:
            componentSize = 4; break;
        default:
            return 0;
    }

    switch (format) {
        case gl.RED:
        case gl.RED_INTEGER:
        case gl.ALPHA:
        case gl.LUMINANCE:
        case gl.DEPTH_COMPONENT:
            return componentSize;
        case gl.RG:
        case gl.RG_INTEGER:
        case gl.LUMINANCE_ALPHA:
            return componentSize * 2;
        case gl.RGB:
        case gl.RGB_INTEGER:
            return componentSize * 3;
        case gl.RGBA:
        case gl.RGBA_INTEGER:
            return componentSize * 4;
        default:
            return 0;
    }
}

export function resizeCanvas(width, height) {
    if (offscreenCanvas && (offscreenCanvas.width !== width || offscreenCanvas.height !== height))
    {
        offscreenCanvas.width = width;
        offscreenCanvas.height = height;
    }
}
