"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.TestRunner = void 0;
const puppeteer = require('puppeteer');
const path = require("path");
const fs = require('fs');
var debug = false;
class TestRunner {
    constructor(page) {
        this._page = page;
        // Create the output screenshots folder
        if (!fs.existsSync('out')) {
            fs.mkdirSync('out');
        }
    }
    runTests() {
        return __awaiter(this, void 0, void 0, function* () {
            // Wait for the app to be initialized
            yield this.waitXamlElement(this._page, "PaneRoot");
            yield this._page.screenshot({ path: 'out/initial_state.png' });
            // Ask for the list of available tests in the running application
            const allTestsData = yield this.getAllTests(this._page);
            console.log(`Running ${allTestsData.length}`);
            let i = 0;
            for (let testName of allTestsData) {
                i += 1;
                console.log("---");
                console.log(`Running ${i}/${allTestsData.length}: ${testName}`);
                // Start the test run
                var testRunId = yield this._page.evaluate(`SampleRunner.RunTest(\'${testName}\')`);
                if (debug) {
                    console.log(`TestID: ${testRunId}`);
                }
                var startDate = new Date();
                while ((startDate - new Date()) < 5000) {
                    // Then wait for the test to be reported as done
                    if (yield this._page.evaluate(`SampleRunner.IsTestDone(\'${testRunId}\')`)) {
                        break;
                    }
                    if (debug) {
                        console.log(`Test not done, waiting...`);
                    }
                    yield this.delay(200);
                }
                yield this._page.screenshot({ path: `out/${testName}.png` });
            }
        });
    }
    getAllTests(page) {
        return __awaiter(this, void 0, void 0, function* () {
            yield page.evaluate("SampleRunner.init()");
            const allTestsData = yield page.evaluate("SampleRunner.GetAllTests()");
            return allTestsData.split(';');
        });
    }
    waitXamlElement(page, xamlName, waitTime = 10000) {
        return __awaiter(this, void 0, void 0, function* () {
            var startDate = new Date();
            while ((new Date() - startDate) < waitTime) {
                yield this.delay(200);
                try {
                    var xamlElement = yield page.$eval(`[xamlname="${xamlName}"]`, a => a);
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
        });
    }
    delay(time) {
        return __awaiter(this, void 0, void 0, function* () {
            return new Promise(function (resolve) {
                setTimeout(resolve, time);
            });
        });
    }
}
exports.TestRunner = TestRunner;
//# sourceMappingURL=TestRunner.js.map