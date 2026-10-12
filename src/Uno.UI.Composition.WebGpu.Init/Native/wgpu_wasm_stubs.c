/* No-op stubs for wgpu-native-only functions that emdawnwebgpu (Dawn) does not export.
 * On WASM the "webgpu" DllImport module is statically linked, so the PInvokeTableGenerator
 * emits references to ALL wgpu* imports in the binding — including these wgpu.h extras that
 * the browser backend never calls (frame sync is browser-driven; no DevicePoll/statistics/etc).
 *
 * Every signature here MUST match its [DllImport] in Native/WebGpuInterop.WgpuNative.cs exactly.
 * The interpreter marshals dynamically and tolerates a mismatch, but an AOT build emits a direct
 * wasm call and the module then fails to validate: the app dies at boot with
 * "Uncaught RuntimeError: function signature mismatch", on every lane, whichever backend it uses.
 * wasm32 mapping: IntPtr/pointer -> void*, uint -> unsigned int, ulong -> unsigned long long,
 * nuint -> size_t, float -> float. */
#include <stddef.h>

void wgpuCommandEncoderClearTexture(void* commandEncoder, void* texture, void* range) { (void)commandEncoder; (void)texture; (void)range; }
void wgpuComputePassEncoderBeginPipelineStatisticsQuery(void* computePassEncoder, void* querySet, unsigned int queryIndex) { (void)computePassEncoder; (void)querySet; (void)queryIndex; }
void wgpuComputePassEncoderEndPipelineStatisticsQuery(void* computePassEncoder) { (void)computePassEncoder; }
void* wgpuDeviceCreateShaderModuleSpirV(void* device, void* descriptor) { (void)device; (void)descriptor; return 0; }
void* wgpuDeviceCreateShaderModuleTrusted(void* device, void* descriptor, unsigned long long runtimeChecks) { (void)device; (void)descriptor; (void)runtimeChecks; return 0; }
/* Called every frame by the shared present session for GPU-completion sync. On the browser this must be a
 * no-op that reports "done" (1): the browser cannot block, and presentation is implicit via requestAnimationFrame. */
unsigned int wgpuDevicePoll(void* device, unsigned int wait, unsigned long long* submissionIndex) { (void)device; (void)wait; (void)submissionIndex; return 1; }
unsigned int wgpuDeviceStartGraphicsDebuggerCapture(void* device) { (void)device; return 0; }
void wgpuDeviceStopGraphicsDebuggerCapture(void* device) { (void)device; }
unsigned int wgpuGetVersion(void) { return 0; }
size_t wgpuInstanceEnumerateAdapters(void* instance, void* options, void* adapters) { (void)instance; (void)options; (void)adapters; return 0; }
float wgpuQueueGetTimestampPeriod(void* queue) { (void)queue; return 0.0f; }
unsigned long long wgpuQueueSubmitForIndex(void* queue, size_t commandCount, void* commands) { (void)queue; (void)commandCount; (void)commands; return 0; }
void wgpuRenderPassEncoderBeginPipelineStatisticsQuery(void* renderPassEncoder, void* querySet, unsigned int queryIndex) { (void)renderPassEncoder; (void)querySet; (void)queryIndex; }
void wgpuRenderPassEncoderEndPipelineStatisticsQuery(void* renderPassEncoder) { (void)renderPassEncoder; }
void wgpuRenderPassEncoderMultiDrawIndexedIndirectCount(void* encoder, void* buffer, unsigned long long offset, void* countBuffer, unsigned long long countBufferOffset, unsigned int maxCount) { (void)encoder; (void)buffer; (void)offset; (void)countBuffer; (void)countBufferOffset; (void)maxCount; }
void wgpuRenderPassEncoderMultiDrawIndirectCount(void* encoder, void* buffer, unsigned long long offset, void* countBuffer, unsigned long long countBufferOffset, unsigned int maxCount) { (void)encoder; (void)buffer; (void)offset; (void)countBuffer; (void)countBufferOffset; (void)maxCount; }
void wgpuSetLogCallback(void* callback, void* userdata) { (void)callback; (void)userdata; }
void wgpuSetLogLevel(unsigned int level) { (void)level; }
