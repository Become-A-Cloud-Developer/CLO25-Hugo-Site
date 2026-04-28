// Validates that the deployed Container App responds and shows the build SHA badge.
// Saves a screenshot to docs/screenshots/<label>.png.
//
// Usage:
//   FQDN=ca-cicd-week4.yellowcoast-76379f35.northeurope.azurecontainerapps.io \
//   LABEL=ex-4.3-passwordless \
//   node scripts/validate.mjs

import { chromium } from "playwright";
import { mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const screenshotDir = join(here, "..", "docs", "screenshots");
mkdirSync(screenshotDir, { recursive: true });

const fqdn = process.env.FQDN;
const label = process.env.LABEL || "validation";
if (!fqdn) {
  console.error("Set FQDN env var (e.g., FQDN=ca-cicd-week4.<env>.azurecontainerapps.io)");
  process.exit(2);
}

const url = `https://${fqdn}/`;
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });

console.log(`Loading ${url}`);
const response = await page.goto(url, { waitUntil: "networkidle", timeout: 30000 });
console.log(`HTTP ${response.status()}`);

const buildSha = (await page.locator("[data-testid='build-sha']").textContent())?.trim();
const hostName = (await page.locator("[data-testid='host-name']").textContent())?.trim();
console.log(`Badges: ${buildSha} | ${hostName}`);

const screenshotPath = join(screenshotDir, `${label}.png`);
await page.screenshot({ path: screenshotPath, fullPage: true });
console.log(`Saved screenshot to ${screenshotPath}`);

await browser.close();

if (response.status() !== 200) {
  console.error(`Expected HTTP 200, got ${response.status()}`);
  process.exit(1);
}
if (!buildSha?.includes("build:")) {
  console.error(`Expected build SHA badge, got: ${buildSha}`);
  process.exit(1);
}
console.log("OK");
