import {
  createInitialListState,
  listFailed,
  listPageChanged,
  listRequested,
  listSucceeded,
} from './list-state';

describe('list-state helpers', () => {
  it('tracks request lifecycle for a paged list', () => {
    const initial = createInitialListState<string>(10);
    const loading = listRequested(initial, { page: 2, pageSize: 10 });

    expect(loading.loading).toBe(true);
    expect(loading.page).toBe(2);
    expect(loading.error).toBeNull();

    const loaded = listSucceeded(loading, {
      items: ['a', 'b'],
      totalCount: 12,
      page: 2,
      pageSize: 10,
    });

    expect(loaded.loading).toBe(false);
    expect(loaded.loaded).toBe(true);
    expect(loaded.items).toEqual(['a', 'b']);
    expect(loaded.totalCount).toBe(12);

    const failed = listFailed(loading, 'boom');
    expect(failed.loading).toBe(false);
    expect(failed.error).toBe('boom');

    const paged = listPageChanged(loaded, 3, 20);
    expect(paged.page).toBe(3);
    expect(paged.pageSize).toBe(20);
  });
});
