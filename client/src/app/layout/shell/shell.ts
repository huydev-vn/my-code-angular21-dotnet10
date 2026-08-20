import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatListItem, MatListItemIcon, MatListItemTitle, MatNavList } from '@angular/material/list';
import { MatSidenav, MatSidenavContainer, MatSidenavContent } from '@angular/material/sidenav';
import { MatToolbar } from '@angular/material/toolbar';

import { IdentityFacade } from '../../features/identity/state/identity.facade';
import { SystemPermissions } from '../../features/identity/models/identity.models';
import { UiFacade } from '../../core/store/ui/ui.facade';

interface NavItem {
  readonly label: string;
  readonly icon: string;
  readonly path: string;
  readonly permission?: string;
}

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatButton,
    MatIconButton,
    MatIcon,
    MatListItem,
    MatListItemIcon,
    MatListItemTitle,
    MatNavList,
    MatSidenav,
    MatSidenavContainer,
    MatSidenavContent,
    MatToolbar,
  ],
  host: {
    class: 'block h-full',
  },
  templateUrl: './shell.html',
  styleUrl: './shell.css',
})
export class Shell {
  private readonly ui = inject(UiFacade);
  private readonly identity = inject(IdentityFacade);

  protected readonly sidenavOpened = this.ui.sidenavOpened;
  protected readonly user = this.identity.user;

  private readonly navItems: readonly NavItem[] = [
    { label: 'Home', icon: 'home', path: '/' },
    {
      label: 'Users',
      icon: 'group',
      path: '/users',
      permission: SystemPermissions.UsersRead,
    },
    {
      label: 'Permissions',
      icon: 'verified_user',
      path: '/authorization/permissions',
      permission: SystemPermissions.AuthorizationPermissionsRead,
    },
    {
      label: 'Groups',
      icon: 'groups',
      path: '/authorization/groups',
      permission: SystemPermissions.AuthorizationGroupsRead,
    },
    {
      label: 'Organization units',
      icon: 'account_tree',
      path: '/authorization/organization-units',
      permission: SystemPermissions.AuthorizationOrganizationUnitsRead,
    },
  ];

  protected readonly visibleNav = computed(() => {
    this.user();
    return this.navItems.filter(
      (item) => !item.permission || this.identity.hasPermission(item.permission),
    );
  });

  protected toggleSidenav(): void {
    this.ui.toggleSidenav();
  }

  protected logout(): void {
    this.identity.logout();
  }
}
