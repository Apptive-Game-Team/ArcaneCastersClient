// node .art/tools/render-cast-kind-icons.cjs (requires sharp)
const fs = require('node:fs');
const path = require('node:path');
const sharp = require('sharp');
const root = path.resolve(__dirname, '../..');
const source = path.join(root, '.art/ui/cast-kind');
const output = path.join(root, 'Assets/Art/Images/UI/Card/CastKind');

async function render() {
    fs.mkdirSync(output, { recursive: true });
    for (const name of ['unit', 'building', 'drop', 'explosion', 'shot']) {
        await sharp(path.join(source, `${name}.svg`), { density: 384 })
            .resize(128, 128).png().toFile(path.join(output, `${name}.png`));
    }
}
render().catch(error => { console.error(error); process.exitCode = 1; });
