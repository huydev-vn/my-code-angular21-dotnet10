import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { MatTableModule } from '@angular/material/table';

import { PageHeader, PagePager, RequestState } from '../../../../shared';
import { UsersFacade } from '../../state/users.facade';

@Component({
  selector: 'app-user-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeader, RequestState, PagePager, MatTableModule],
  templateUrl: './user-list-page.html',
  styleUrl: './user-list-page.css',
})
export class UserListPage implements OnInit {
  private readonly usersFacade = inject(UsersFacade);

  protected readonly users = this.usersFacade.users;
  protected readonly loading = this.usersFacade.loading;
  protected readonly error = this.usersFacade.error;
  protected readonly page = this.usersFacade.page;
  protected readonly pageSize = this.usersFacade.pageSize;
  protected readonly totalCount = this.usersFacade.totalCount;
  protected readonly columns = ['email', 'groups'];

  ngOnInit(): void {
    this.usersFacade.loadIfNeeded();
  }

  protected retry(): void {
    this.usersFacade.reload();
  }

  protected onPageChange(event: { page: number; pageSize: number }): void {
    this.usersFacade.changePage(event.page, event.pageSize);
  }
}
