namespace Uno.UI.Runtime.Skia {
	export class BrowserNativeElementHostingExtension {
		public static isNativeElement(content: string): boolean {
			return document.getElementById(content) instanceof HTMLElement;
		}

		public static attachNativeElement(content: string) {
			let nativeElementHost = document.getElementById("uno-native-element-host");
			if (nativeElementHost === undefined || nativeElementHost === null) {
				nativeElementHost = document.createElement("div");
				nativeElementHost.id = "uno-native-element-host";
				nativeElementHost.style.position = "absolute";
				nativeElementHost.style.height = "100%";
				nativeElementHost.style.width = "100%";
				nativeElementHost.style.zIndex = "-1";
				nativeElementHost.style.overflow = "hidden";
				nativeElementHost.style.pointerEvents = "none";
				let unoBody = document.getElementById("uno-body");
				unoBody.insertBefore(nativeElementHost, unoBody.firstChild)
			}
			let element = document.getElementById(content);
			nativeElementHost.appendChild(element);
		}

		public static detachNativeElement(content: string) {
			let element = document.getElementById(content) as HTMLElement;
			element.remove();
		}

		public static arrangeNativeElement(content: string, x: number, y: number, width: number, height: number) {
			let element = document.getElementById(content) as HTMLElement;
			element.style.position = "absolute"
			element.style.left = `${x}px`;
			element.style.top = `${y}px`;
			element.style.width = `${width}px`;
			element.style.height = `${height}px`;
		}

		public static changeNativeElementOpacity(content: string, opacity: number) {
			let element = document.getElementById(content) as HTMLElement;
		}

		public static createSampleComponent(s: string): string {
			let element = document.createElement("div");
			let btn = document.createElement("button");
			btn.textContent = s;
			btn.style.display = "inline-block";
			btn.style.width = "100%";
			btn.style.height = "100%";
			btn.style.backgroundColor = "#ff69b4"; /* Hot pink */
			btn.style.color = "white";
			btn.addEventListener("click", _ => alert(`button ${s} clicked`));
			element.appendChild(btn);

			element.id = (Math.random() + 1).toString(36).substring(7);
			document.body.appendChild(element);
			return element.id;
		}
	}
}
