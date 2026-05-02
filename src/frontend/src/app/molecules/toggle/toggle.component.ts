import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';

@Component({
  selector: 'app-toggle',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <label class="inline-flex items-center gap-2 cursor-pointer select-none">
      <button
        data-testid="toggle"
        role="switch"
        type="button"
        [attr.aria-checked]="checked()"
        [attr.aria-label]="label()"
        (click)="toggled.emit(!checked())"
        class="relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[--color-primary]"
        [class.bg-[--color-primary]]="checked()"
        [class.bg-[--color-border]]="!checked()"
      >
        <span
          class="inline-block h-4 w-4 transform rounded-full bg-white shadow-md transition-transform"
          [class.translate-x-6]="checked()"
          [class.translate-x-1]="!checked()"
        ></span>
      </button>
      @if (label()) {
        <span class="text-sm text-[--color-text-primary]">{{ label() }}</span>
      }
    </label>
  `,
})
export class ToggleComponent {
  readonly checked = input(false);
  readonly label = input('');
  readonly toggled = output<boolean>();
}
