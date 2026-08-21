import type { SystemPermission } from '../../../core/auth/system-permissions';
import { SystemPermissions } from '../../../core/auth/system-permissions';
import type { CurrentUser } from '../../../core/auth/current-user.model';

export interface AccessTokenResponseDto {
  accessToken: string;
  accessTokenExpiresAt: string;
}

export interface UserResponseDto {
  id: string;
  email: string;
  createdAt: string;
  groups: readonly string[];
  permissions: readonly string[];
  accessibleOrganizationUnitIds: readonly string[];
}

const knownPermissions = new Set<string>(Object.values(SystemPermissions));

export function isSystemPermission(value: string): value is SystemPermission {
  return knownPermissions.has(value);
}

export function mapAccessTokenResponse(dto: unknown): AccessTokenResponseDto {
  if (!isRecord(dto)) {
    throw new Error('Invalid access token response.');
  }

  const accessToken = dto['accessToken'];
  const accessTokenExpiresAt = dto['accessTokenExpiresAt'];
  if (typeof accessToken !== 'string' || accessToken.length === 0) {
    throw new Error('Access token response is missing accessToken.');
  }

  if (typeof accessTokenExpiresAt !== 'string' || accessTokenExpiresAt.length === 0) {
    throw new Error('Access token response is missing accessTokenExpiresAt.');
  }

  return { accessToken, accessTokenExpiresAt };
}

export function mapUserResponse(dto: unknown): CurrentUser {
  if (!isRecord(dto)) {
    throw new Error('Invalid user response.');
  }

  const id = dto['id'];
  const email = dto['email'];
  const groups = dto['groups'];
  const permissions = dto['permissions'];
  const accessibleOrganizationUnitIds = dto['accessibleOrganizationUnitIds'];

  if (typeof id !== 'string' || id.length === 0) {
    throw new Error('User response is missing id.');
  }

  if (typeof email !== 'string' || email.length === 0) {
    throw new Error('User response is missing email.');
  }

  if (!Array.isArray(groups) || !groups.every((item) => typeof item === 'string')) {
    throw new Error('User response has invalid groups.');
  }

  if (
    !Array.isArray(permissions) ||
    !permissions.every((item) => typeof item === 'string')
  ) {
    throw new Error('User response has invalid permissions.');
  }

  if (
    !Array.isArray(accessibleOrganizationUnitIds) ||
    !accessibleOrganizationUnitIds.every(
      (item) => typeof item === 'string' || typeof item === 'number',
    )
  ) {
    throw new Error('User response has invalid accessibleOrganizationUnitIds.');
  }

  return {
    id,
    email,
    groups,
    permissions: permissions.filter(isSystemPermission),
    accessibleOrganizationUnitIds: accessibleOrganizationUnitIds.map(String),
  };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
