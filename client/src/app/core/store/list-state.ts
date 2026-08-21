import type { PageQuery, PageResult } from '../http/page-result.model';

/** Shared pagination / request metadata for list features. */
export interface PagedQueryState {
  loading: boolean;
  loaded: boolean;
  error: string | null;
  page: number;
  pageSize: number;
  totalCount: number;
}

/** Paged list slice with in-memory items (non-entity collections). */
export interface ListState<T> extends PagedQueryState {
  items: readonly T[];
}

export function createInitialPagedQueryState(pageSize = 20): PagedQueryState {
  return {
    loading: false,
    loaded: false,
    error: null,
    page: 1,
    pageSize,
    totalCount: 0,
  };
}

export function createInitialListState<T>(pageSize = 20): ListState<T> {
  return {
    ...createInitialPagedQueryState(pageSize),
    items: [],
  };
}

export function pagedQueryRequested(
  state: PagedQueryState,
  query?: Partial<PageQuery>,
): PagedQueryState {
  return {
    ...state,
    loading: true,
    error: null,
    page: query?.page ?? state.page,
    pageSize: query?.pageSize ?? state.pageSize,
  };
}

export function pagedQuerySucceeded(
  state: PagedQueryState,
  page: Pick<PageResult<unknown>, 'totalCount' | 'page' | 'pageSize'>,
): PagedQueryState {
  return {
    ...state,
    loading: false,
    loaded: true,
    error: null,
    totalCount: page.totalCount,
    page: page.page,
    pageSize: page.pageSize,
  };
}

export function pagedQueryFailed(state: PagedQueryState, error: string): PagedQueryState {
  return {
    ...state,
    loading: false,
    error,
  };
}

export function pagedQueryPageChanged(
  state: PagedQueryState,
  page: number,
  pageSize: number,
): PagedQueryState {
  return {
    ...state,
    page,
    pageSize,
  };
}

export function listRequested<T>(
  state: ListState<T>,
  query?: Partial<PageQuery>,
): ListState<T> {
  return {
    ...state,
    ...pagedQueryRequested(state, query),
  };
}

export function listSucceeded<T>(
  state: ListState<T>,
  result: PageResult<T>,
): ListState<T> {
  return {
    ...state,
    ...pagedQuerySucceeded(state, result),
    items: result.items,
  };
}

export function listFailed<T>(state: ListState<T>, error: string): ListState<T> {
  return {
    ...state,
    ...pagedQueryFailed(state, error),
  };
}

export function listPageChanged<T>(
  state: ListState<T>,
  page: number,
  pageSize: number,
): ListState<T> {
  return {
    ...state,
    ...pagedQueryPageChanged(state, page, pageSize),
  };
}
