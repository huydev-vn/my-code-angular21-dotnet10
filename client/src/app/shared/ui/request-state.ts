import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatProgressBar } from '@angular/material/progress-bar';

import { EmptyState } from './empty-state';

@Component({
  selector: 'app-request-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EmptyState, MatButton, MatProgressBar],
  template: `
    @if (loading()) {
      <mat-progress-bar
        mode="indeterminate"
        class="mb-4"
        [attr.aria-label]="loadingLabel()"
      />
      <p class="sr-only" aria-live="polite">{{ loadingLabel() }}</p>
    }

    @if (error(); as message) {
      <div
        class="mb-4 rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-700"
        role="alert"
        aria-live="assertive"
      >
        <p>{{ message }}</p>
        @if (showRetry()) {
          <button matButton type="button" class="mt-3" (click)="retry.emit()">Try again</button>
        }
      </div>
    }

    @if (!loading() && !error() && empty()) {
      <app-empty-state [title]="emptyTitle()" [description]="emptyDescription()" />
    } @else if (!loading() && !error() && !empty()) {
      <ng-content />
    }
  `,
})
export class RequestState {
  readonly loading = input(false);
  readonly error = input<string | null>(null);
  readonly empty = input(false);
  readonly emptyTitle = input('No data');
  readonly emptyDescription = input('');
  readonly loadingLabel = input('Loading content');
  readonly showRetry = input(true);

  readonly retry = output<void>();
}
