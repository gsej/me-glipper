import { Injectable } from '@angular/core';

/** Single owner of the API key stored in localStorage. */
@Injectable({ providedIn: 'root' })
export class ApiKeyService {
  private readonly storageKey = 'api_key';

  get(): string | null {
    return localStorage.getItem(this.storageKey);
  }

  set(value: string): void {
    localStorage.setItem(this.storageKey, value);
  }
}
