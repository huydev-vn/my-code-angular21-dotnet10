import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatListModule } from '@angular/material/list';
import { MatProgressBar } from '@angular/material/progress-bar';

import { EmptyState, PageHeader } from '../../../../shared';
import { AuthorizationFacade } from '../../state/authorization.facade';

@Component({
  selector: 'app-permission-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EmptyState, PageHeader, MatListModule, MatProgressBar],
  templateUrl: './permission-list-page.html',
  styleUrl: './permission-list-page.css',
})
export class PermissionListPage {
  private readonly authorization = inject(AuthorizationFacade);

  protected readonly permissions = this.authorization.permissions;
  protected readonly loading = this.authorization.loading;
  protected readonly error = this.authorization.error;
}
