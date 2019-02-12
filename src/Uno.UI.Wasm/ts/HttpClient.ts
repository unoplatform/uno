namespace Uno.Http {
	export interface IHttpClientConfig {
		id: string;
		method: string;
		url: string;
		headers?: string[] [];
		payload?: string;
		payloadType?: string;
		cacheMode?: RequestCache;
	}

	export class HttpClient {

		public static async send(config: IHttpClientConfig) {

			const params: RequestInit = {
				method: config.method,
				cache: config.cacheMode || "default",
				headers: new Headers(config.headers)
			};

			if (config.payload) {
				params.body = await this.blobFromBase64(config.payload, config.payloadType);
			}

			try {
				const response = await fetch(config.url, params);

				let responseHeaders = "";
				response.headers.forEach(
					(v: string, k: string) => responseHeaders += `${k}:${v}\n`
				);

				const responseBlob = await response.blob();
				const responsePayload = responseBlob ? await this.base64FromBlob(responseBlob) : "";

				this.dispatchResponse(config.id, response.status, responseHeaders, responsePayload);


			} catch (error) {
				this.dispatchError(config.id, `${error.message || error}`);
				console.error(error);
			}
		}

		private static async blobFromBase64(base64: string, contentType: string): Promise<Blob> {
			contentType = contentType || "application/octet-stream";
			const url = `data:${contentType};base64,${base64}`;
			return await (await fetch(url)).blob();
		}

		private static base64FromBlob(blob: Blob): Promise<string> {
			return new Promise<string>(resolve => {
				const reader = new FileReader();
				reader.onloadend = () => {
					const dataUrl: string = reader.result as string;
					const base64 = dataUrl.split(",", 2)[1];
					resolve(base64);
				};
				reader.readAsDataURL(blob);
			});
		}

		private static dispatchResponse(requestId: string, status: number, headers: string, payload: string) {

			this.initMethods();

			const requestIdStr = MonoRuntime.mono_string(requestId);
			const statusStr = MonoRuntime.mono_string("" + status);
			const headersStr = MonoRuntime.mono_string(headers);
			const payloadStr = MonoRuntime.mono_string(payload);
			MonoRuntime.call_method(this.dispatchResponseMethod, null, [requestIdStr, statusStr, headersStr, payloadStr]);
		}

		private static dispatchError(requestId: string, error: string) {

			this.initMethods();

			const requestIdStr = MonoRuntime.mono_string(requestId);
			const errorStr = MonoRuntime.mono_string(error);
			MonoRuntime.call_method(this.dispatchErrorMethod, null, [requestIdStr, errorStr]);
		}

		private static dispatchResponseMethod: any;
		private static dispatchErrorMethod: any;

		private static initMethods() {
			if (this.dispatchResponseMethod) {
				return; // already initialized.
			}

			const asm = MonoRuntime.assembly_load("Uno.UI.Wasm");
			const httpClass = MonoRuntime.find_class(asm, "Uno.UI.Wasm", "WasmHttpHandler");
			this.dispatchResponseMethod = MonoRuntime.find_method(httpClass, "DispatchResponse", -1);
			this.dispatchErrorMethod = MonoRuntime.find_method(httpClass, "DispatchError", -1);
		}
	}
}
