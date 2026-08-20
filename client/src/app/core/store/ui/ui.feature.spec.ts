import { UiActions } from './ui.actions';
import { uiFeature, uiInitialState } from './ui.feature';

describe('uiFeature reducer', () => {
  it('toggles the sidenav', () => {
    const closed = uiFeature.reducer(uiInitialState, UiActions.toggleSidenav());
    expect(closed.sidenavOpened).toBe(false);

    const opened = uiFeature.reducer(closed, UiActions.toggleSidenav());
    expect(opened.sidenavOpened).toBe(true);
  });
});
