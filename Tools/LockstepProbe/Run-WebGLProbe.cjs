const { chromium } = require('playwright');
const fs = require('node:fs');

// 별도 브라우저 프로필로 빌드를 실행하고 실제 IL2CPP 출력만 기록한다.
async function main() {
    const [url, executablePath, output] = process.argv.slice(2);
    if (!url || !executablePath || !output) throw new Error('Usage: node Run-WebGLProbe.cjs URL BROWSER LOG');
    fs.writeFileSync(output, '');
    const browser = await chromium.launch({ executablePath, headless: true,
        args: ['--enable-unsafe-swiftshader', '--use-angle=swiftshader'] });
    let timer;
    let paritySummary;
    let skipped = 0;
    try {
        const page = await browser.newPage();
        const finished = new Promise((resolve, reject) => {
            timer = setTimeout(() => reject(new Error('WebGL probe timed out after 300 seconds')), 300000);
            page.on('console', message => {
                const text = message.text();
                fs.appendFileSync(output, text + '\n');
                if (text.startsWith('PARITY SKIP ')) skipped++;
                if (text.startsWith('PASS ported tests=')) paritySummary = text;
                if (text.includes('PASS ticks=10000 traceSha256=')) {
                    if (!paritySummary) reject(new Error('Missing original-test summary'));
                    else resolve(paritySummary + '; skipped=' + skipped + '\n' + text);
                }
                if (text.startsWith('FAIL ')) reject(new Error(text));
            });
            page.on('pageerror', error => {
                fs.appendFileSync(output, 'PAGE ERROR ' + error + '\n');
                reject(error);
            });
            page.on('crash', () => reject(new Error('WebGL page crashed')));
        });
        // 페이지를 여는 중 실패해도 rejection을 미처리 상태로 두지 않는다.
        finished.catch(() => {});
        await page.goto(url, { waitUntil: 'domcontentloaded' });
        console.log(await finished);
    } finally {
        clearTimeout(timer);
        await browser.close();
    }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
