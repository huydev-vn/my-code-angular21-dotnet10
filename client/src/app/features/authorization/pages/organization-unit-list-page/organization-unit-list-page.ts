import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatListModule } from '@angular/material/list';
import { MatProgressBar } from '@angular/material/progress-bar';

import { EmptyState, PageHeader } from '../../../../shared';
import { AuthorizationFacade } from '../../state/authorization.facade';

@Component({
  selector: 'app-organization-unit-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EmptyState, PageHeader, MatListModule, MatProgressBar],
  templateUrl: './organization-unit-list-page.html',
  styleUrl: './organization-unit-list-page.css',
})
export class OrganizationUnitListPage {
  private readonly authorization = inject(AuthorizationFacade);

  protected readonly organizationUnits = this.authorization.organizationUnits;
  protected readonly loading = this.authorization.loading;
}
