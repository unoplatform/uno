module Uno.UI.Interop {
	export interface IUnoDispatch {
		resize(size: string): void;
		dispatch(htmlIdStr: string, eventNameStr: string, eventPayloadStr: string): string;
	}
}
