namespace Uno.UI.Runtime.Skia {
	export class ImageLoader {
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
			const canvas = document.createElement('canvas');
			canvas.width = img.width;
			canvas.height = img.height;

			// All the code below is standard WebGL boilerplate to draw an image
			// except for the fragement shader which has an alpha multiplication step.
			const gl = canvas.getContext("webgl")
			gl.viewport(0, 0, canvas.width, canvas.height);

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
				const shader = gl.createShader(type);
				gl.shaderSource(shader, source);
				gl.compileShader(shader);
				return shader;
			}

			const program = gl.createProgram();
			gl.attachShader(program, createShader(gl.VERTEX_SHADER, vertexShaderSource));
			gl.attachShader(program, createShader(gl.FRAGMENT_SHADER, fragmentShaderSource));
			gl.linkProgram(program);
			gl.useProgram(program);

			const vertices = new Float32Array([
				-1, -1, 0, 0,  // Bottom left
				1, -1, 1, 0,  // Bottom right
				-1,  1, 0, 1,  // Top left
				1,  1, 1, 1   // Top right
			]);

			const vertexBuffer = gl.createBuffer();
			gl.bindBuffer(gl.ARRAY_BUFFER, vertexBuffer);
			gl.bufferData(gl.ARRAY_BUFFER, vertices, gl.STATIC_DRAW);

			const positionLocation = gl.getAttribLocation(program, 'a_position');
			gl.enableVertexAttribArray(positionLocation);
			gl.vertexAttribPointer(positionLocation, 2, gl.FLOAT, false, 16, 0);

			const texcoordLocation = gl.getAttribLocation(program, 'a_texcoord');
			gl.enableVertexAttribArray(texcoordLocation);
			gl.vertexAttribPointer(texcoordLocation, 2, gl.FLOAT, false, 16, 8);

			const texture = gl.createTexture();
			gl.bindTexture(gl.TEXTURE_2D, texture);

			gl.bindTexture(gl.TEXTURE_2D, texture);
			gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);

			gl.clear(gl.COLOR_BUFFER_BIT);
			gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);

			gl.flush();

			const pixelData = new Uint8Array(img.width * img.height * 4);

			gl.readPixels(0, 0, img.width, img.height, gl.RGBA, gl.UNSIGNED_BYTE, pixelData);

			canvas.width = 0;
			canvas.height = 0;
			
			return Array.from<number>(pixelData);
		}
	}
}
