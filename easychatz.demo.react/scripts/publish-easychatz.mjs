// Publishes EasyChatz.CustomElements, whose output vite.config.js serves at /_framework and /_content.
// Skips the publish when output already exists, unless run with --force (npm run easychatz).
import { execFileSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { easyChatzOutput, easyChatzProject, easyChatzPublishDir } from './paths.mjs';

const force = process.argv.includes('--force');

if (!force && existsSync(easyChatzOutput)) {
    console.log('EasyChatz custom element already published. Run "npm run easychatz" to republish.');
    process.exit(0);
}

console.log('Publishing the EasyChatz custom element...');
execFileSync('dotnet', ['publish', easyChatzProject, '-c', 'Release', '-o', easyChatzPublishDir, '--nologo'], { stdio: 'inherit' });
