namespace Uno.UI.Runtime.Skia {
	export class ImageLoader {
		static canvas: HTMLCanvasElement = document.createElement('canvas');
		static gl: WebGLRenderingContext = ImageLoader.canvas.getContext("webgl")
		
		public static async loadFromArray(array: Uint8Array): Promise<object> {
			return new Promise<object>((resolve) => {
				const image = new Blob([array]);
				const imageUrl = URL.createObjectURL(image);

				const img = new Image();
				img.onload = () => {
					URL.revokeObjectURL(imageUrl);

					const bytes = ImageLoader.imageToBytes(img);

					resolve({
						error: null,
						bytes: bytes,
						width: img.width,
						height: img.height
					});
				};

				img.onerror = e => {
					URL.revokeObjectURL(imageUrl);
					resolve({error: e.toString()});
				};

				img.src = imageUrl;
			});
		}

		// We get the bytes of an image by first drawing it on a canvas with webgl
		// then reading the pixels from the canvas into a buffer. This would be
		// a lot easier with the '2d' canvas API that has a drawImage function,
		// but due to a bug in Skia on WASM, we need the pixels to be
		// alpha-premultiplied because using SKAlphaType.Unpremul is not working
		// correctly (see also https://github.com/unoplatform/uno/issues/20727),
		// so we multiply the RGB values by the alpha by hand in the fragement buffer
		// instead. This is very likely slower than the '2d' canvas API, but we
		// have to do this way to do the alpha premultiplication.
		private static imageToBytes(img: HTMLImageElement): Array<number> {
			ImageLoader.canvas.width = img.width;
			ImageLoader.canvas.height = img.height;

			// All the code below is standard WebGL boilerplate to draw an image
			// except for the fragement shader which has an alpha multiplication step.
			ImageLoader.gl.viewport(0, 0, ImageLoader.canvas.width, ImageLoader.canvas.height);

			const vertexShaderSource = `
				attribute vec4 a_position;
				attribute vec2 a_texcoord;
				varying vec2 v_texcoord;
				
				void main() {
					gl_Position = a_position;
					v_texcoord = a_texcoord;
				}
			`;
			
			const fragmentShaderSource = `
				precision mediump float;
				varying vec2 v_texcoord;
				uniform sampler2D u_texture;
				
				void main() {
					gl_FragColor = texture2D(u_texture, v_texcoord);
					// INTERESTING PART HERE -------------------------------------------------------------------------------
					gl_FragColor = vec4(gl_FragColor.r * gl_FragColor.a, gl_FragColor.g * gl_FragColor.a, gl_FragColor.b * gl_FragColor.a, gl_FragColor.a);
				}
			`;

			// @ts-ignore
			function createShader(type, source) {
				const shader = ImageLoader.gl.createShader(type);
				ImageLoader.gl.shaderSource(shader, source);
				ImageLoader.gl.compileShader(shader);
				return shader;
			}

			const program = ImageLoader.gl.createProgram();
			ImageLoader.gl.attachShader(program, createShader(ImageLoader.gl.VERTEX_SHADER, vertexShaderSource));
			ImageLoader.gl.attachShader(program, createShader(ImageLoader.gl.FRAGMENT_SHADER, fragmentShaderSource));
			ImageLoader.gl.linkProgram(program);
			ImageLoader.gl.useProgram(program);

			const vertices = new Float32Array([
				-1, -1, 0, 0,  // Bottom left
				1, -1, 1, 0,  // Bottom right
				-1,  1, 0, 1,  // Top left
				1,  1, 1, 1   // Top right
			]);

			const vertexBuffer = ImageLoader.gl.createBuffer();
			ImageLoader.gl.bindBuffer(ImageLoader.gl.ARRAY_BUFFER, vertexBuffer);
			ImageLoader.gl.bufferData(ImageLoader.gl.ARRAY_BUFFER, vertices, ImageLoader.gl.STATIC_DRAW);

			const positionLocation = ImageLoader.gl.getAttribLocation(program, 'a_position');
			ImageLoader.gl.enableVertexAttribArray(positionLocation);
			ImageLoader.gl.vertexAttribPointer(positionLocation, 2, ImageLoader.gl.FLOAT, false, 16, 0);

			const texcoordLocation = ImageLoader.gl.getAttribLocation(program, 'a_texcoord');
			ImageLoader.gl.enableVertexAttribArray(texcoordLocation);
			ImageLoader.gl.vertexAttribPointer(texcoordLocation, 2, ImageLoader.gl.FLOAT, false, 16, 8);

			const texture = ImageLoader.gl.createTexture();
			ImageLoader.gl.bindTexture(ImageLoader.gl.TEXTURE_2D, texture);

			ImageLoader.gl.texParameteri(ImageLoader.gl.TEXTURE_2D, ImageLoader.gl.TEXTURE_WRAP_S, ImageLoader.gl.CLAMP_TO_EDGE);
			ImageLoader.gl.texParameteri(ImageLoader.gl.TEXTURE_2D, ImageLoader.gl.TEXTURE_WRAP_T, ImageLoader.gl.CLAMP_TO_EDGE);
			ImageLoader.gl.texParameteri(ImageLoader.gl.TEXTURE_2D, ImageLoader.gl.TEXTURE_MIN_FILTER, ImageLoader.gl.LINEAR);
			ImageLoader.gl.texParameteri(ImageLoader.gl.TEXTURE_2D, ImageLoader.gl.TEXTURE_MAG_FILTER, ImageLoader.gl.LINEAR);

			ImageLoader.gl.bindTexture(ImageLoader.gl.TEXTURE_2D, texture);
			ImageLoader.gl.texImage2D(ImageLoader.gl.TEXTURE_2D, 0, ImageLoader.gl.RGBA, ImageLoader.gl.RGBA, ImageLoader.gl.UNSIGNED_BYTE, img);

			ImageLoader.gl.clear(ImageLoader.gl.COLOR_BUFFER_BIT);
			ImageLoader.gl.drawArrays(ImageLoader.gl.TRIANGLE_STRIP, 0, 4);

			ImageLoader.gl.flush();

			const pixelData = new Uint8Array(img.width * img.height * 4);

			ImageLoader.gl.readPixels(0, 0, img.width, img.height, ImageLoader.gl.RGBA, ImageLoader.gl.UNSIGNED_BYTE, pixelData);

			ImageLoader.canvas.width = 0;
			ImageLoader.canvas.height = 0;
			
			return Array.from<number>(pixelData);
		}
	}
}
