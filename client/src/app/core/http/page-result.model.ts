export interface PageQuery {
  page: number;
  pageSize: number;
  sort?: string;
  search?: string;
}

export interface PageResult<T> {
  items: readonly T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export function createEmptyPageResult<T>(query: PageQuery): PageResult<T> {
  return {
    items: [],
    totalCount: 0,
    page: query.page,
    pageSize: query.pageSize,
  };
}
