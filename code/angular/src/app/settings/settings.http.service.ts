import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { SettingsService } from "./settings.service";
import { Settings } from "./settings";
import { firstValueFrom } from "rxjs";

/** Thrown when settings.json cannot be loaded, so bootstrap can show a specific message. */
export class SettingsLoadError extends Error {
  constructor(public readonly reason: unknown) {
    super("settings.json could not be loaded");
    this.name = "SettingsLoadError";
  }
}

@Injectable({ providedIn: 'root' })
export class SettingsHttpService {

  constructor(private http: HttpClient, private settingsService: SettingsService) {
  }

  initializeApp(): Promise<void> {
    // Reject (rather than hang) if settings.json is missing: apiUrl lives there, so the app
    // cannot function without it. main.ts catches this and shows a message instead of a blank page.
    return firstValueFrom(this.http.get<Settings>('settings.json'))
      .then(settings => { this.settingsService.settings = settings; })
      .catch(reason => { throw new SettingsLoadError(reason); });
  }
}
