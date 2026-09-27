import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'app-pi',
  imports: [],
  template: `
    <div class="flex items-center justify-between h-10 select-none w-full">
      @if (isTextVisible && swaggerUrl) {
        <a [href]="swaggerUrl" target="_blank" rel="noopener" class="text-sm"
          >swagger</a
        >
      }
      <div
        class="flex items-center cursor-pointer ml-auto"
        (click)="toggleText()"
      >
        <span class="mr-2" [class.hidden]="!isTextVisible">{{ text }}</span>
        <span class="text-lg">&pi;</span>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PiComponent {
  @Input() text = '';
  @Input() swaggerUrl = '';
  isTextVisible = false;

  toggleText() {
    this.isTextVisible = !this.isTextVisible;
  }
}
