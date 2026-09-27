import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { SettingsLoadError } from './app/settings/settings.http.service';

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => {
    console.error(err);
    if (err instanceof SettingsLoadError) {
      showFatalError('settings.json is missing or could not be loaded.');
    }
  });

// Bootstrap failed, so Angular never rendered — paint a message directly into the DOM.
// Inline styles so it shows even if the stylesheet is unavailable.
function showFatalError(message: string): void {
  const root = document.querySelector('app-root');
  if (!root) {
    return;
  }
  root.innerHTML = `
    <div style="position:fixed;inset:0;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:8px;padding:24px;text-align:center;font-family:sans-serif;color:#111827">
      <h1 style="font-size:1.25rem;font-weight:600;margin:0">Configuration error</h1>
      <p style="color:#4b5563;margin:0">${message}</p>
    </div>`;
}
