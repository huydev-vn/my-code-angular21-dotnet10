import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { MatListModule } from '@angular/material/list';

import { PageHeader, PagePager, RequestState } from '../../../../shared';
import { AuthorizationFacade } from '../../state/authorization.facade';

@Component({
  selector: 'app-organization-unit-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeader, RequestState, PagePager, MatListModule],
  templateUrl: './organization-unit-list-page.html',
  styleUrl: './organization-unit-list-page.css',
})
export class OrganizationUnitListPage implements OnInit {
  private readonly authorization = inject(AuthorizationFacade);

  protected readonly organizationUnits = this.authorization.organizationUnits;
  protected readonly loading = this.authorization.organizationUnitsLoading;
  protected readonly error = this.authorization.organizationUnitsError;
  protected readonly page = this.authorization.organizationUnitsPage;
  protected readonly pageSize = this.authorization.organizationUnitsPageSize;
  protected readonly totalCount = this.authorization.organizationUnitsTotalCount;

  ngOnInit(): void {
    this.authorization.loadOrganizationUnitsIfNeeded();
  }

  protected retry(): void {
    this.authorization.reloadOrganizationUnits();
  }

  protected onPageChange(event: { page: number; pageSize: number }): void {
    this.authorization.changeOrganizationUnitsPage(event.page, event.pageSize);
  }
}
