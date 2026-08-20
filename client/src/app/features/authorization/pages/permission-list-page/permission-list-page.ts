import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatListModule } from '@angular/material/list';

import { PageHeader, RequestState } from '../../../../shared';
import { AuthorizationFacade } from '../../state/authorization.facade';

@Component({
  selector: 'app-permission-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeader, RequestState, MatListModule],
  templateUrl: './permission-list-page.html',
  styleUrl: './permission-list-page.css',
})
export class PermissionListPage {
  private readonly authorization = inject(AuthorizationFacade);

  protected readonly permissions = this.authorization.permissions;
  protected readonly loading = this.authorization.loading;
  protected readonly error = this.authorization.error;

  protected retry(): void {
    this.authorization.reloadCatalog();
  }
}
