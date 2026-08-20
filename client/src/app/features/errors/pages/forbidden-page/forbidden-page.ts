import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';

import { PageHeader } from '../../../../shared';

@Component({
  selector: 'app-forbidden-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeader, MatButton, RouterLink],
  template: `
    <app-page-header
      title="Access denied"
      subtitle="You do not have permission to view this page."
    />

    <div class="rounded-lg border border-slate-200 bg-white p-6">
      <p class="text-sm text-slate-600">
        Contact an administrator if you believe this is a mistake.
      </p>
      <a matButton="filled" routerLink="/" class="mt-4 inline-flex">Back to home</a>
    </div>
  `,
})
export class ForbiddenPage {}
