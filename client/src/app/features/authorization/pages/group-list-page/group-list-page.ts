import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatListModule } from '@angular/material/list';

import { PageHeader, RequestState } from '../../../../shared';
import { AuthorizationFacade } from '../../state/authorization.facade';

@Component({
  selector: 'app-group-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeader, RequestState, MatListModule],
  templateUrl: './group-list-page.html',
  styleUrl: './group-list-page.css',
})
export class GroupListPage {
  private readonly authorization = inject(AuthorizationFacade);

  protected readonly groups = this.authorization.groups;
  protected readonly loading = this.authorization.loading;
  protected readonly error = this.authorization.error;

  protected retry(): void {
    this.authorization.reloadCatalog();
  }
}
