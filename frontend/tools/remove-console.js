const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const extWhitelist = ['.ts', '.js', '.tsx', '.jsx', '.html'];

function walk(dir) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const e of entries) {
    const full = path.join(dir, e.name);
    if (e.isDirectory()) {
      if (['node_modules', '.git', 'dist', 'out'].includes(e.name)) continue;
      walk(full);
    } else {
      const ext = path.extname(e.name).toLowerCase();
      if (!extWhitelist.includes(ext)) continue;
      // Only process files under src or tools
      if (!full.includes(path.sep + 'src' + path.sep) && !full.includes(path.sep + 'tools' + path.sep)) continue;
      try {
        let content = fs.readFileSync(full, 'utf8');
        // Remove lines containing console.log
        const before = content;
        content = content.split(/\r?\n/).filter(line => !/\bconsole\.log\s*\(/.test(line)).join('\n');
        // Remove excessive consecutive empty lines
        content = content.replace(/\n{3,}/g, '\n\n');
        if (content !== before) {
          fs.writeFileSync(full, content, 'utf8');
        }
      } catch (err) {
        console.error('Error processing', full, err.message);
      }
    }
  }
}

walk(root);
