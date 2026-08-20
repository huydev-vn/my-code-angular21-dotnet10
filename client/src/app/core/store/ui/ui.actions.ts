import { createActionGroup, emptyProps } from '@ngrx/store';

export const UiActions = createActionGroup({
  source: 'UI',
  events: {
    'Toggle Sidenav': emptyProps(),
    'Open Sidenav': emptyProps(),
    'Close Sidenav': emptyProps(),
  },
});
