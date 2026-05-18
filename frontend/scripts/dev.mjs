import { realpathSync } from 'node:fs';
import { spawn } from 'node:child_process';
import path from 'node:path';

const realCwd = realpathSync(process.cwd());
if (realCwd !== process.cwd()) {
  process.chdir(realCwd);
}

const viteBin = path.join(
  realCwd,
  'node_modules',
  '.bin',
  process.platform === 'win32' ? 'vite.cmd' : 'vite',
);

const child = spawn(viteBin, process.argv.slice(2), {
  cwd: realCwd,
  stdio: 'inherit',
  shell: process.platform === 'win32',
});

child.on('exit', (code) => process.exit(code ?? 0));
child.on('error', (err) => {
  console.error(err);
  process.exit(1);
});
