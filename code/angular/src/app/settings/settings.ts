/**
 * App settings, baked into the bundle at build time (`ng build --define GIT_HASH=... --define
 * API_URL=...`, see src/build-constants.d.ts), so the reported UI hash always matches the code
 * that is running. Without the defines (local dev) the fallbacks below apply.
 */
export class Settings {
  gitHash = typeof GIT_HASH !== 'undefined' ? GIT_HASH : 'local';
  apiUrl = typeof API_URL !== 'undefined' ? API_URL : 'http://localhost:5200';
}
