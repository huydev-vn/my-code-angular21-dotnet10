import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatListModule } from '@angular/material/list';

import { PageHeader, RequestState } from '../../../../shared';
import { AuthorizationFacade } from '../../state/authorization.facade';

@Component({
  selector: 'app-organization-unit-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeader, RequestState, MatListModule],
  templateUrl: './organization-unit-list-page.html',
  styleUrl: './organization-unit-list-page.css',
})
export class OrganizationUnitListPage {
  private readonly authorization = inject(AuthorizationFacade);

  protected readonly organizationUnits = this.authorization.organizationUnits;
  protected readonly loading = this.authorization.loading;
  protected readonly error = this.authorization.error;

  protected retry(): void {
    this.authorization.reloadCatalog();
  }
}
