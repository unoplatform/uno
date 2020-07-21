namespace MSAL {
	export class WebUI {

		public static authenticate(
			urlNavigate: string,
			urlRedirect: string,
			title: string,
			popUpWidth: number,
			popUpHeight: number): Promise<string> {

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
						err("Incompleted");
					}
				};

				const completeSuccessfully = (url: string) => {
					if (!finished) {
						ok(url);
						finished = true;
						close();
					}
				};
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
						completeWithError("Cant open window");
						return;
					}
					if (win.focus) {
						win.focus();
					}
					win.addEventListener("beforeunload", close);

					const onFinalUrlReached = (success: boolean, finalUrlOrMessage: string) => {
						if (success) {
							completeSuccessfully(finalUrlOrMessage);
						} else {
							completeWithError(finalUrlOrMessage);
						}
					}
					timerSubscription = WebUI.startMonitoringRedirect(win, urlRedirect, onFinalUrlReached);
				} catch (e) {
					completeWithError(`${e}`);
				}
			});
		}

		private static startMonitoringRedirect(
			win: Window,
			urlRedirect: string,
			callback: (success: boolean, finalUrlOrMessage: string) => void): number {

			const subscription = window.setInterval(() => {
				try {
					if (win.closed) {
						callback(false, "Popup closed");
						return;
					}
					const url = win.document.URL;
					if (url.indexOf(urlRedirect) !== -1) {
						callback(true, url);
					}
				} catch (e) {
					// normal! DOMException / crossed origin until reached correct redirect page
				}
			},
				100);

			return subscription;
		}

	}

}
