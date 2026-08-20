import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIcon],
  template: `
    <div class="flex flex-col items-center justify-center gap-2 py-16 text-center text-slate-500">
      <mat-icon class="scale-125">{{ icon() }}</mat-icon>
      <p class="text-base font-medium text-slate-700">{{ title() }}</p>
      @if (description()) {
        <p class="max-w-md text-sm">{{ description() }}</p>
      }
    </div>
  `,
})
export class EmptyState {
  readonly title = input.required<string>();
  readonly description = input('');
  readonly icon = input('inbox');
}
