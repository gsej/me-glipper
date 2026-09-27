import { Routes } from '@angular/router';

// The app renders its pages directly in AppComponent and navigates via query params
// (?page=...), so there are no routed components. The empty-path route gives the wildcard a
// valid target; the wildcard catches unknown deep links (e.g. a refresh on a stale path) and
// sends them back to the app root instead of erroring.
export const routes: Routes = [
  { path: '', pathMatch: 'full', children: [] },
  { path: '**', redirectTo: '' },
];
