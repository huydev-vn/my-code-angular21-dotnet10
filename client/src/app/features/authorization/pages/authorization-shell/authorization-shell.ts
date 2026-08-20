import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AuthorizationFacade } from '../../state/authorization.facade';

@Component({
  selector: 'app-authorization-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet],
  template: '<router-outlet />',
  styleUrl: './authorization-shell.css',
})
export class AuthorizationShell implements OnInit {
  private readonly authorization = inject(AuthorizationFacade);

  ngOnInit(): void {
    this.authorization.loadCatalog();
  }
}
