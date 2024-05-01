namespace Microsoft.UI.Xaml.Controls {
	export class TextBlock {
		public static getSelectedText(htmlId: number): string {
			var element = document.getElementById(htmlId.toString());
			if (element) {
				var selection = window.getSelection();
				if (selection && selection.rangeCount > 0) {
					var range = selection.getRangeAt(0);
					if (element.contains(range.startContainer) && element.contains(range.endContainer)) {
						return range.toString();
					}
				}
			}
		}

		public static select(htmlId : number, from: number, length: number): void {
			var element = document.getElementById(htmlId.toString());
			if (element) {
				var selection = window.getSelection();
				var range = document.createRange();
				range.setStart(element.childNodes[0], from);
				range.setEnd(element.childNodes[0], from + length);
				selection.removeAllRanges();
				selection.addRange(range);
			}
		}
	}
}
