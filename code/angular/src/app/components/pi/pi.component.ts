import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  Input,
  OnInit,
  inject,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiKeyService } from '../../services/api-key.service';

const GITHUB_URL = 'https://github.com/gsej/me-glipper';

/**
 * A small π button that opens a settings modal: the API key, the UI and API git hashes, and
 * links to the API's Swagger page and the GitHub repo. The modal opens by itself on startup
 * when no API key is stored, since π is the only way to enter one.
 */
@Component({
  selector: 'app-pi',
  imports: [FormsModule],
  template: `
    <button
      type="button"
      class="text-lg text-gray-600 select-none px-1"
      aria-label="Settings"
      (click)="open()"
    >
      &pi;
    </button>

    @if (isOpen) {
      <!-- Backdrop: tapping it closes the modal. Marked so swipes on it never change page. -->
      <div
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4"
        data-no-page-swipe
        (click)="close()"
      >
        <div
          class="w-full max-w-xs rounded-lg bg-white p-4 shadow-lg"
          role="dialog"
          aria-modal="true"
          aria-labelledby="pi-dialog-title"
          (click)="$event.stopPropagation()"
        >
          <div class="mb-3 flex items-center justify-between">
            <h3 id="pi-dialog-title" class="text-base font-semibold text-gray-800">Settings</h3>
            <button
              type="button"
              class="-mr-2 flex h-8 w-8 items-center justify-center rounded text-gray-400 hover:text-gray-700"
              aria-label="Close"
              (click)="close()"
            >
              <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                stroke-width="2" stroke-linecap="round" aria-hidden="true">
                <path d="M6 6l12 12M18 6L6 18" />
              </svg>
            </button>
          </div>

          <label for="pi-api-key" class="mb-1 block text-sm text-gray-500">API key</label>
          <input
            id="pi-api-key"
            type="text"
            [(ngModel)]="apiKey"
            (change)="saveApiKey()"
            placeholder="Enter API key"
            autocomplete="off"
            class="mb-4 w-full rounded border border-gray-300 px-2 py-1 text-sm focus:border-blue-500 focus:outline-none"
          />

          <dl class="mb-3 grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 text-sm">
            <dt class="text-gray-500">UI</dt>
            <dd class="font-mono text-gray-800">{{ uiHash }}</dd>
            <dt class="text-gray-500">API</dt>
            <dd class="font-mono text-gray-800">{{ apiHash }}</dd>
          </dl>

          <div class="flex flex-col gap-2 text-sm">
            @if (swaggerUrl) {
              <a [href]="swaggerUrl" target="_blank" rel="noopener" class="flex items-center gap-2 text-blue-500">
                <svg class="h-4 w-4 flex-shrink-0" viewBox="0 0 128 128" aria-hidden="true">
                  <path fill="#85ea2d" d="M63.999 124.945c-33.607 0-60.95-27.34-60.95-60.949C3.05 30.388 30.392 3.048 64 3.048s60.95 27.342 60.95 60.95c0 33.607-27.343 60.946-60.95 60.946z"/>
                  <path fill="#173647" d="M40.3 43.311c-.198 2.19.072 4.454-.073 6.668-.173 2.217-.444 4.407-.888 6.596-.615 3.126-2.56 5.489-5.24 7.458 5.218 3.396 5.807 8.662 6.152 14.003.172 2.88.098 5.785.394 8.638.221 2.215 1.082 2.782 3.372 2.854.935.025 1.894 0 2.978 0v6.842c-6.768 1.156-12.354-.762-13.734-6.496a39.329 39.329 0 0 1-.836-6.4c-.148-2.287.097-4.577-.074-6.864-.492-6.277-1.305-8.393-7.308-8.689v-7.8c.441-.1.86-.174 1.302-.223 3.298-.172 4.701-1.182 5.414-4.43a37.512 37.512 0 0 0 .616-5.536c.247-3.569.148-7.21.763-10.754.86-5.094 4.01-7.556 9.254-7.852 1.476-.074 2.978 0 4.676 0v6.99c-.714.05-1.33.147-1.969.147-4.258-.148-4.48 1.304-4.8 4.848zm8.195 16.193h-.099c-2.462-.123-4.578 1.796-4.702 4.258-.122 2.485 1.797 4.603 4.259 4.724h.295c2.436.148 4.527-1.724 4.676-4.16v-.245c.05-2.486-1.944-4.527-4.43-4.577zm15.43 0c-2.386-.074-4.38 1.796-4.454 4.159 0 .149 0 .271.024.418 0 2.684 1.821 4.406 4.578 4.406 2.707 0 4.406-1.772 4.406-4.553-.025-2.682-1.823-4.455-4.554-4.43Zm15.801 0a4.596 4.596 0 0 0-4.676 4.454 4.515 4.515 0 0 0 4.528 4.528h.05c2.264.394 4.553-1.796 4.701-4.429.122-2.437-2.092-4.553-4.604-4.553Zm21.682.369c-2.855-.123-4.284-1.083-4.996-3.79a27.444 27.444 0 0 1-.811-5.292c-.198-3.298-.174-6.62-.395-9.918-.516-7.826-6.177-10.557-14.397-9.205v6.792c1.304 0 2.313 0 3.322.025 1.748.024 3.077.69 3.249 2.634.172 1.772.172 3.568.344 5.365.346 3.57.542 7.187 1.157 10.706.542 2.904 2.536 5.07 5.02 6.841-4.355 2.929-5.636 7.113-5.857 11.814-.122 3.223-.196 6.472-.368 9.721-.148 2.953-1.181 3.913-4.16 3.987-.835.024-1.648.098-2.583.148v6.964c1.748 0 3.347.1 4.946 0 4.971-.295 7.974-2.706 8.96-7.531.417-2.658.662-5.34.737-8.023.171-2.46.148-4.946.394-7.382.369-3.815 2.116-5.389 5.93-5.636a5.161 5.161 0 0 0 1.06-.245v-7.801c-.64-.074-1.084-.148-1.552-.173zM64 6.1c31.977 0 57.9 25.92 57.9 57.898 0 31.977-25.923 57.899-57.9 57.899-31.976 0-57.898-25.922-57.898-57.9C6.102 32.023 32.024 6.101 64 6.101m0-6.1C28.71 0 0 28.71 0 64c0 35.288 28.71 63.998 64 63.998 35.289 0 64-28.71 64-64S99.289.002 64 .002Z"/>
                </svg>
                Swagger
              </a>
            }
            <a [href]="githubUrl" target="_blank" rel="noopener" class="flex items-center gap-2 text-blue-500">
              <svg class="h-4 w-4 flex-shrink-0" viewBox="0 0 25 24" fill="#24292f" aria-hidden="true">
                <path d="M12.3047 0C5.50634 0 0 5.50942 0 12.3047C0 17.7423 3.52529 22.3535 8.41332 23.9787C9.02856 24.0946 9.25414 23.7142 9.25414 23.3871C9.25414 23.0949 9.24389 22.3207 9.23876 21.2953C5.81601 22.0377 5.09414 19.6444 5.09414 19.6444C4.53427 18.2243 3.72524 17.8449 3.72524 17.8449C2.61064 17.082 3.81137 17.0973 3.81137 17.0973C5.04697 17.1835 5.69604 18.3647 5.69604 18.3647C6.79321 20.2463 8.57636 19.7029 9.27978 19.3881C9.39052 18.5924 9.70736 18.0499 10.0591 17.7423C7.32641 17.4347 4.45429 16.3765 4.45429 11.6618C4.45429 10.3185 4.9311 9.22133 5.72065 8.36C5.58222 8.04931 5.16694 6.79833 5.82831 5.10337C5.82831 5.10337 6.85883 4.77319 9.2121 6.36459C10.1965 6.09082 11.2424 5.95546 12.2883 5.94931C13.3342 5.95546 14.3801 6.09082 15.3644 6.36459C17.7023 4.77319 18.7328 5.10337 18.7328 5.10337C19.3942 6.79833 18.9789 8.04931 18.8559 8.36C19.6403 9.22133 20.1171 10.3185 20.1171 11.6618C20.1171 16.3888 17.2409 17.4296 14.5031 17.7321C14.9338 18.1012 15.3337 18.8559 15.3337 20.0084C15.3337 21.6552 15.3183 22.978 15.3183 23.3779C15.3183 23.7009 15.5336 24.0854 16.1642 23.9623C21.0871 22.3484 24.6094 17.7341 24.6094 12.3047C24.6094 5.50942 19.0999 0 12.3047 0Z" />
              </svg>
              GitHub
            </a>
          </div>
        </div>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PiComponent implements OnInit {
  private readonly apiKeyService = inject(ApiKeyService);

  @Input() uiHash = '';
  @Input() apiHash = '';
  @Input() swaggerUrl = '';
  readonly githubUrl = GITHUB_URL;
  apiKey = '';
  isOpen = false;

  ngOnInit() {
    // No key stored: open straight away, as this is the only place to enter one.
    if (!this.apiKeyService.get()) {
      this.open();
    }
  }

  open() {
    this.apiKey = this.apiKeyService.get() ?? '';
    this.isOpen = true;
  }

  close() {
    this.isOpen = false;
  }

  saveApiKey() {
    this.apiKeyService.set(this.apiKey);
  }

  @HostListener('document:keydown.escape')
  onEscape() {
    this.close();
  }
}
