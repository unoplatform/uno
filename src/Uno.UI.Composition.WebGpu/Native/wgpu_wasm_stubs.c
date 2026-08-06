/* No-op stubs for wgpu-native-only functions that emdawnwebgpu (Dawn) does not export.
 * On WASM the "webgpu" DllImport module is statically linked, so the PInvokeTableGenerator
 * emits references to ALL wgpu* imports in the binding — including these wgpu.h extras that
 * the browser backend never calls (frame sync is browser-driven; no DevicePoll/statistics/etc).
 * These definitions exist only to satisfy the static link; calling them on WASM is a no-op. */
unsigned long long wgpuCommandEncoderClearTexture() { return 0; }
unsigned long long wgpuComputePassEncoderBeginPipelineStatisticsQuery() { return 0; }
unsigned long long wgpuComputePassEncoderEndPipelineStatisticsQuery() { return 0; }
unsigned long long wgpuDeviceCreateShaderModuleSpirV() { return 0; }
unsigned long long wgpuDeviceCreateShaderModuleTrusted() { return 0; }
unsigned long long wgpuDevicePoll() { return 0; }
unsigned long long wgpuDeviceStartGraphicsDebuggerCapture() { return 0; }
unsigned long long wgpuDeviceStopGraphicsDebuggerCapture() { return 0; }
unsigned long long wgpuGetVersion() { return 0; }
unsigned long long wgpuInstanceEnumerateAdapters() { return 0; }
unsigned long long wgpuQueueGetTimestampPeriod() { return 0; }
unsigned long long wgpuQueueSubmitForIndex() { return 0; }
unsigned long long wgpuRenderPassEncoderBeginPipelineStatisticsQuery() { return 0; }
unsigned long long wgpuRenderPassEncoderEndPipelineStatisticsQuery() { return 0; }
unsigned long long wgpuRenderPassEncoderMultiDrawIndexedIndirectCount() { return 0; }
unsigned long long wgpuRenderPassEncoderMultiDrawIndirectCount() { return 0; }
unsigned long long wgpuSetLogCallback() { return 0; }
unsigned long long wgpuSetLogLevel() { return 0; }
