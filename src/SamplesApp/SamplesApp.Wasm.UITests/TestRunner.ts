const puppeteer = require('puppeteer');
const path = require("path");
const fs = require('fs');

var debug = false;

export class TestRunner {

	_page: any;

	constructor(page: any) {
		this._page = page;

		// Create the output screenshots folder
		if (!fs.existsSync('out')) {
			fs.mkdirSync('out');
		}
	}

	async runTests() {

		// Wait for the app to be initialized
		await this.waitXamlElement(this._page, "PaneRoot");

		await this._page.screenshot({ path: 'out/initial_state.png' });

		// Ask for the list of available tests in the running application
		const allTestsData = await this.getAllTests(this._page);

		console.log(`Running ${allTestsData.length}`);

		let i = 0;

		for (let testName of allTestsData) {
			i += 1;

			console.log("---")
			console.log(`Running ${i}/${allTestsData.length}: ${testName}`);
			// Start the test run
			var testRunId = await this._page.evaluate(`SampleRunner.RunTest(\'${testName}\')`);

			if (debug) {
				console.log(`TestID: ${testRunId}`);
			}

			var startDate = new Date();

			while ((<any>startDate - <any>new Date()) < 5000) {

				// Then wait for the test to be reported as done
				if (await this._page.evaluate(`SampleRunner.IsTestDone(\'${testRunId}\')`)) {
					break;
				}

				if (debug) {
					console.log(`Test not done, waiting...`);
				}

				await this.delay(200);
			}

			await this._page.screenshot({ path: `out/${testName}.png` });
		}
	}

	private async getAllTests(page: any): Promise<Array<string>> {
		await page.evaluate("SampleRunner.init()");
		const allTestsData = await page.evaluate("SampleRunner.GetAllTests()") as String;

		return allTestsData.split(';');
	}

	private async waitXamlElement(page: any, xamlName: string, waitTime: number = 10000): Promise<any> {
		var startDate = new Date();

		while ((<any>new Date() - <any>startDate) < waitTime) {
			await this.delay(200);
			try {
				var xamlElement = await page.$eval(`[xamlname="${xamlName}"]`, a => a);
				if (debug) {
					console.log(`Got xamlElement [${xamlName}]`);
				}
				return xamlElement;
			}
			catch (e) {
				if (debug) {
					console.log(`Waiting for [${xamlName}] (${e})`);
				}
			}
		}

		throw `Failed to get [${xamlName}]`;
	}

	private async delay(time) {
		return new Promise(function (resolve) {
			setTimeout(resolve, time)
		});
	}
}
