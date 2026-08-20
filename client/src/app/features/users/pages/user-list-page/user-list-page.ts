import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { MatTableModule } from '@angular/material/table';

import { RequestState } from '../../../../shared';
import { PageHeader } from '../../../../shared';
import { UsersFacade } from '../../state/users.facade';

@Component({
  selector: 'app-user-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeader, RequestState, MatTableModule],
  templateUrl: './user-list-page.html',
  styleUrl: './user-list-page.css',
})
export class UserListPage implements OnInit {
  private readonly usersFacade = inject(UsersFacade);

  protected readonly users = this.usersFacade.users;
  protected readonly loading = this.usersFacade.loading;
  protected readonly error = this.usersFacade.error;
  protected readonly columns = ['email', 'groups'];

  ngOnInit(): void {
    this.usersFacade.loadIfNeeded();
  }

  protected retry(): void {
    this.usersFacade.reload();
  }
}
