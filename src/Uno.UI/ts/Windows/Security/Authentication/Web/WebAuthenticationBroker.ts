namespace Windows.Security.Authentication.Web {
	export class WebAuthenticationBroker {

		public static getReturnUrl() {
			return window.location.origin;
		}

		public static authenticateUsingIframe(
			iframeId: string,
			urlNavigate: string,
			urlRedirect: string,
			timeout: number
		) {
			return new Promise<string>((ok, err) => {
				let iframe: HTMLIFrameElement;

				if (iframeId) {
					iframe = document.getElementById(iframeId) as HTMLIFrameElement;
				}

				if (!iframe) {
					iframe = document.createElement("iframe");
					iframe.style.opacity = "0";
					iframe.style.pointerEvents = "none";
					document.body.append(iframe);
				}

				const terminate = () => {
					iframe.removeEventListener("load", onload);
					iframe.src = "about:blank";
					if (!iframeId) {
						iframe.remove();
					}
				}

				const onload = () => {
					if (!iframe.contentDocument) {
						return; // can't access right now
					}

					const currentUrl = iframe.contentDocument.URL;
					console.log("iframe src=" + currentUrl);

					if (currentUrl.indexOf(urlRedirect) === 0) {
						terminate();
						ok(`success|${currentUrl}`);
					}
				};

				iframe.addEventListener("load", onload);

				iframe.src = urlNavigate;
			})
				;
		}

		public static authenticateUsingWindow(
			urlNavigate: string,
			urlRedirect: string,
			title: string,
			popUpWidth: number,
			popUpHeight: number,
			timeout: number): Promise<string> {

			let win: Window = null;
			let timerSubscription: number = null;

			return new Promise<string>((ok, err) => {

				let finished = false;
				const close = () => {
					if (win) {
						win.close();
						win = null;
					}

					if (timerSubscription) {
						window.clearInterval(timerSubscription);
						timerSubscription = null;
					}
					if (!finished) {
						err("Incomplete");
					}
				};

				const completeSuccessfully = (url: string) => {
					if (!finished) {
						ok(`success|${url}`);
						finished = true;
						close();
					}
				};

				const completeUserCancel = () => {
					if (!finished) {
						ok(`cancel`);
						finished = true;
						close();
					}
				}

				const completeTimeout = () => {
					if (!finished) {
						ok(`timeout`);
						finished = true;
						close();
					}
				}

				const completeWithError = (error: string) => {
					if (!finished) {
						err(error);
						finished = true;
						close();
					}
				};

				try {
					/**
					 * adding winLeft and winTop to account for dual monitor
					 * using screenLeft and screenTop for IE8 and earlier
					 */
					const winLeft = window.screenLeft ? window.screenLeft : window.screenX;
					const winTop = window.screenTop ? window.screenTop : window.screenY;
					/**
					 * window.innerWidth displays browser window"s height and width excluding toolbars
					 * using document.documentElement.clientWidth for IE8 and earlier
					 */
					const width = window.innerWidth ||
						document.documentElement.clientWidth ||
						document.body.clientWidth;
					const height = window.innerHeight ||
						document.documentElement.clientHeight ||
						document.body.clientHeight;
					const left = ((width / 2) - (popUpWidth / 2)) + winLeft;
					const top = ((height / 2) - (popUpHeight / 2)) + winTop;

					// open the window
					win = window.open(urlNavigate,
						title,
						"width=" + popUpWidth + ", height=" + popUpHeight + ", top=" + top + ", left=" + left);
					if (!win) {
						completeWithError("Can't open window");
						return;
					}
					if (win.focus) {
						win.focus();
					}
					win.addEventListener("beforeunload", close);

					const onFinalUrlReached = (success: boolean, timedout: boolean, finalUrlOrMessage: string) => {
						if (success) {
							completeSuccessfully(finalUrlOrMessage);
						} else {
							if (timedout) {
								completeTimeout();
							} else {
								completeUserCancel();
							}
						}
					}
					timerSubscription = this.startMonitoringRedirect(win, urlRedirect, timeout, onFinalUrlReached);
				} catch (e) {
					completeWithError(`${e}`);
				}
			});
		}

		private static startMonitoringRedirect(
			win: Window,
			urlRedirect: string,
			timeout: number,
			callback: (success: boolean, timedout: boolean, finalUrlOrMessage: string) => void): number {

			const currentTime = (new Date()).getTime();
			const maxTime = currentTime + timeout;

			const subscription = window.setInterval(() => {
					try {
						if ((new Date()).getTime() > maxTime) {
							callback(false, true, null);
						}

						if (win.closed) {
							callback(false, false, "Popup closed");
							return;
						}
						const url = win.document.URL;
						if (url.indexOf(urlRedirect) === 0) {
							callback(true, false, url);
						}

					} catch (e) {
						// Expected! DOMException / crossed origin until reached correct redirect page
					}
				},
				100);

			return subscription;
		}
	}
}
