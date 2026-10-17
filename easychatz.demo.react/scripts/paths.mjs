import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');

export const easyChatzProject = path.join(root, 'EasyChatz.CustomElements', 'EasyChatz.CustomElements.csproj');
export const easyChatzPublishDir = path.join(root, 'EasyChatz.CustomElements', 'bin', 'publish');
export const easyChatzOutput = path.join(easyChatzPublishDir, 'wwwroot');
