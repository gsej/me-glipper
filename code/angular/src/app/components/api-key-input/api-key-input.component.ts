import {
  Component,
  OnInit,
  HostListener,
  ChangeDetectionStrategy,
  inject,
} from '@angular/core';

import { FormsModule } from '@angular/forms';
import { ApiKeyService } from '../../services/api-key.service';

@Component({
  selector: 'app-api-key-input',
  imports: [FormsModule],
  templateUrl: './api-key-input.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './api-key-input.component.scss',
})
export class ApiKeyInputComponent implements OnInit {
  private readonly apiKeyService = inject(ApiKeyService);

  apiKey: string = '';
  isVisible: boolean = false;

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    const apiKeyInput = document.querySelector('app-api-key-input');

    // Only hide if clicking outside the component
    if (apiKeyInput && !apiKeyInput.contains(target)) {
      this.isVisible = false;
    }
  }

  ngOnInit() {
    this.apiKey = this.apiKeyService.get() ?? '';
  }

  toggleVisibility() {
    this.isVisible = !this.isVisible;
  }

  saveApiKey() {
    this.apiKeyService.set(this.apiKey);
  }
}
