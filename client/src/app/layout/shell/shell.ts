import { BreakpointObserver } from '@angular/cdk/layout';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatListItem, MatListItemIcon, MatListItemTitle, MatNavList } from '@angular/material/list';
import { MatSidenav, MatSidenavContainer, MatSidenavContent } from '@angular/material/sidenav';
import { MatToolbar } from '@angular/material/toolbar';
import { filter, map } from 'rxjs';

import { AUTH_COMMANDS, AUTH_STATE, SystemPermission, SystemPermissions } from '../../core';
import { UiFacade } from '../../core/store/ui/ui.facade';

interface NavItem {
  readonly label: string;
  readonly icon: string;
  readonly path: string;
  readonly permission?: SystemPermission;
}

interface NavGroup {
  readonly id: string;
  readonly label: string;
  readonly items: readonly NavItem[];
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
  private readonly authState = inject(AUTH_STATE);
  private readonly authCommands = inject(AUTH_COMMANDS);
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly appName = 'Workspace';
  protected readonly sidenavOpened = this.ui.sidenavOpened;
  protected readonly user = this.authState.user;
  protected readonly isHandset = toSignal(
    this.breakpointObserver.observe('(max-width: 768px)').pipe(map((result) => result.matches)),
    { initialValue: false },
  );
  protected readonly sidenavMode = computed(() => (this.isHandset() ? 'over' : 'side'));

  private readonly navGroups: readonly NavGroup[] = [
    {
      id: 'overview',
      label: 'Overview',
      items: [{ label: 'Home', icon: 'home', path: '/' }],
    },
    {
      id: 'directory',
      label: 'Directory',
      items: [
        {
          label: 'Users',
          icon: 'group',
          path: '/users',
          permission: SystemPermissions.UsersRead,
        },
      ],
    },
    {
      id: 'authorization',
      label: 'Authorization',
      items: [
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
      ],
    },
  ];

  protected readonly visibleNavGroups = computed(() =>
    this.navGroups
      .map((group) => ({
        ...group,
        items: group.items.filter(
          (item) => !item.permission || this.authState.hasPermission(item.permission),
        ),
      }))
      .filter((group) => group.items.length > 0),
  );

  constructor() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        if (this.isHandset()) {
          this.ui.closeSidenav();
        }
      });
  }

  protected toggleSidenav(): void {
    this.ui.toggleSidenav();
  }

  protected logout(): void {
    this.authCommands.logout();
  }
}
