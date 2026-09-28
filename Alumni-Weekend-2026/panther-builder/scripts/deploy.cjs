const { execSync } = require('child_process');
const path = require('path');
const fs = require('fs');

console.log('1. Building production bundle...');
execSync('npm run build', { cwd: path.resolve(__dirname, '..'), stdio: 'inherit' });

console.log('2. Publishing dist directory to gh-pages branch...');
const distDir = path.resolve(__dirname, '..', 'dist');
const gitCmd = (cmd) => execSync(cmd, { cwd: distDir, stdio: 'inherit' });
const gitDir = path.join(distDir, '.git');
if (!fs.existsSync(gitDir)) {
  gitCmd('git init');
  gitCmd('git remote add origin https://github.com/jcallinan/Alumni-Weekend-2026.git');
} else {
  try {
    gitCmd('git remote set-url origin https://github.com/jcallinan/Alumni-Weekend-2026.git');
  } catch (e) {}
}
gitCmd('git checkout -B gh-pages');
gitCmd('git add -A');
gitCmd('git commit -m "deploy: update WebXR Panther build" --allow-empty');
gitCmd('git push -f origin gh-pages');
console.log('=== Successfully deployed to gh-pages branch! https://jcallinan.github.io/Alumni-Weekend-2026/ ===');