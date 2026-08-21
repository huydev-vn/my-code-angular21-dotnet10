import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AUTH_COMMANDS } from './core/auth/auth-state.port';
import { App } from './app';

describe('App', () => {
  const bootstrap = vi.fn();

  beforeEach(async () => {
    bootstrap.mockClear();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        {
          provide: AUTH_COMMANDS,
          useValue: {
            bootstrap,
            logout: vi.fn(),
          },
        },
      ],
    }).compileComponents();
  });

  it('should create the app and bootstrap auth', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    expect(fixture.componentInstance).toBeTruthy();
    expect(bootstrap).toHaveBeenCalledTimes(1);
  });
});
