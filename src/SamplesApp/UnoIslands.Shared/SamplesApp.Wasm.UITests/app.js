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
const TestRunner_1 = require("./TestRunner");
const puppeteer = require('puppeteer');
const path = require("path");
const fs = require('fs');
(() => __awaiter(void 0, void 0, void 0, function* () {
    console.log(`Init puppeteer`);
    const browser = yield puppeteer.launch({
        "headless": true,
        args: ['--no-sandbox', '--disable-setuid-sandbox'],
        "defaultViewport": { "width": 1280, "height": 1024 }
    });
    const page = yield browser.newPage();
    page.on('console', msg => {
        console.log('BROWSER LOG:', msg.text());
    });
    page.on('requestfailed', err => console.error('BROWSER-REQUEST-FAILED:', err));
    yield page.goto("http://localhost:8000/");
    var runner = new TestRunner_1.TestRunner(page);
    yield runner.runTests();
    console.log(`Tests done`);
    yield browser.close();
}))();
//# sourceMappingURL=app.js.map