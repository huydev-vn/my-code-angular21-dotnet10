import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCard, MatCardContent, MatCardHeader, MatCardTitle } from '@angular/material/card';

import { AUTH_STATE } from '@core';
import { PageHeader } from '@shared';

@Component({
  selector: 'app-home-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, MatCard, MatCardContent, MatCardHeader, MatCardTitle, PageHeader],
  templateUrl: './home-page.html',
  styleUrl: './home-page.css',
})
export class HomePage {
  private readonly authState = inject(AUTH_STATE);

  protected readonly user = this.authState.user;
}
