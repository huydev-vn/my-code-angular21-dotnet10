import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { MatListModule } from '@angular/material/list';

import { PageHeader, PagePager, RequestState } from '../../../../shared';
import { AuthorizationFacade } from '../../state/authorization.facade';

@Component({
  selector: 'app-permission-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeader, RequestState, PagePager, MatListModule],
  templateUrl: './permission-list-page.html',
  styleUrl: './permission-list-page.css',
})
export class PermissionListPage implements OnInit {
  private readonly authorization = inject(AuthorizationFacade);

  protected readonly permissions = this.authorization.permissions;
  protected readonly loading = this.authorization.permissionsLoading;
  protected readonly error = this.authorization.permissionsError;
  protected readonly page = this.authorization.permissionsPage;
  protected readonly pageSize = this.authorization.permissionsPageSize;
  protected readonly totalCount = this.authorization.permissionsTotalCount;

  ngOnInit(): void {
    this.authorization.loadPermissionsIfNeeded();
  }

  protected retry(): void {
    this.authorization.reloadPermissions();
  }

  protected onPageChange(event: { page: number; pageSize: number }): void {
    this.authorization.changePermissionsPage(event.page, event.pageSize);
  }
}
