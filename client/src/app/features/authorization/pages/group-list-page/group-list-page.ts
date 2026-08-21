import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { MatListModule } from '@angular/material/list';

import { PageHeader, PagePager, RequestState } from '../../../../shared';
import { AuthorizationFacade } from '../../state/authorization.facade';

@Component({
  selector: 'app-group-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeader, RequestState, PagePager, MatListModule],
  templateUrl: './group-list-page.html',
  styleUrl: './group-list-page.css',
})
export class GroupListPage implements OnInit {
  private readonly authorization = inject(AuthorizationFacade);

  protected readonly groups = this.authorization.groups;
  protected readonly loading = this.authorization.groupsLoading;
  protected readonly error = this.authorization.groupsError;
  protected readonly page = this.authorization.groupsPage;
  protected readonly pageSize = this.authorization.groupsPageSize;
  protected readonly totalCount = this.authorization.groupsTotalCount;

  ngOnInit(): void {
    this.authorization.loadGroupsIfNeeded();
  }

  protected retry(): void {
    this.authorization.reloadGroups();
  }

  protected onPageChange(event: { page: number; pageSize: number }): void {
    this.authorization.changeGroupsPage(event.page, event.pageSize);
  }
}
