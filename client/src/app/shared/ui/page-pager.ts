import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';

@Component({
  selector: 'app-page-pager',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatPaginatorModule],
  template: `
    <mat-paginator
      class="mt-3"
      [length]="totalCount()"
      [pageIndex]="page() - 1"
      [pageSize]="pageSize()"
      [pageSizeOptions]="pageSizeOptions()"
      [disabled]="disabled()"
      [showFirstLastButtons]="true"
      (page)="onPage($event)"
      aria-label="Pagination"
    />
  `,
})
export class PagePager {
  readonly totalCount = input.required<number>();
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly disabled = input(false);
  readonly pageSizeOptions = input<readonly number[]>([10, 20, 50, 100]);

  readonly pageChange = output<{ page: number; pageSize: number }>();

  protected onPage(event: PageEvent): void {
    this.pageChange.emit({
      page: event.pageIndex + 1,
      pageSize: event.pageSize,
    });
  }
}
