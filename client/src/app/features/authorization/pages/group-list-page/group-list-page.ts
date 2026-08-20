import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatListModule } from '@angular/material/list';
import { MatProgressBar } from '@angular/material/progress-bar';

import { EmptyState, PageHeader } from '../../../../shared';
import { AuthorizationFacade } from '../../state/authorization.facade';

@Component({
  selector: 'app-group-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EmptyState, PageHeader, MatListModule, MatProgressBar],
  templateUrl: './group-list-page.html',
  styleUrl: './group-list-page.css',
})
export class GroupListPage {
  private readonly authorization = inject(AuthorizationFacade);

  protected readonly groups = this.authorization.groups;
  protected readonly loading = this.authorization.loading;
}
