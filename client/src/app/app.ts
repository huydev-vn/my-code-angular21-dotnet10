import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AUTH_COMMANDS } from './core/auth/auth-state.port';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet],
  templateUrl: './app.html',
  host: {
    class: 'block h-full',
  },
})
export class App implements OnInit {
  private readonly authCommands = inject(AUTH_COMMANDS);

  ngOnInit(): void {
    this.authCommands.bootstrap();
  }
}
