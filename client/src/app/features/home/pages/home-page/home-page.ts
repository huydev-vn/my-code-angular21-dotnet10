import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCard, MatCardContent, MatCardHeader, MatCardTitle } from '@angular/material/card';

import { IdentityFacade } from '../../../identity/state/identity.facade';
import { PageHeader } from '../../../../shared';

@Component({
  selector: 'app-home-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, MatCard, MatCardContent, MatCardHeader, MatCardTitle, PageHeader],
  templateUrl: './home-page.html',
  styleUrl: './home-page.css',
})
export class HomePage {
  private readonly identity = inject(IdentityFacade);

  protected readonly user = this.identity.user;
}
