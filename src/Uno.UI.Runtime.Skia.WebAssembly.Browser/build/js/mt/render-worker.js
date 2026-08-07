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

export function resizeCanvas(width, height) {
    if (offscreenCanvas && (offscreenCanvas.width !== width || offscreenCanvas.height !== height))
    {
        offscreenCanvas.width = width;
        offscreenCanvas.height = height;
    }
}
