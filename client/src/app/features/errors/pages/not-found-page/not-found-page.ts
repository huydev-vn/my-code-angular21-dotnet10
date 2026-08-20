import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';

import { PageHeader } from '../../../../shared/ui/page-header';

@Component({
  selector: 'app-not-found-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeader, MatButton, RouterLink],
  template: `
    <app-page-header title="Page not found" subtitle="The page you requested does not exist." />

    <div class="rounded-lg border border-slate-200 bg-white p-6">
      <p class="text-sm text-slate-600">Check the URL or return to the workspace home page.</p>
      <a matButton="filled" routerLink="/" class="mt-4 inline-flex">Back to home</a>
    </div>
  `,
})
export class NotFoundPage {}
