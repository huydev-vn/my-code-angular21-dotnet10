import { Injectable } from '@angular/core';

export interface AccessTokenSession {
  readonly accessToken: string;
  readonly accessTokenExpiresAt: string;
}

/**
 * Holds the access token in memory only. Refresh tokens are delivered via
 * HttpOnly cookies and are never readable from JavaScript.
 */
@Injectable({ providedIn: 'root' })
export class TokenSession {
  private accessToken: string | null = null;
  private accessTokenExpiresAt: string | null = null;

  setAccessToken(session: AccessTokenSession): void {
    this.accessToken = session.accessToken;
    this.accessTokenExpiresAt = session.accessTokenExpiresAt;
  }

  clear(): void {
    this.accessToken = null;
    this.accessTokenExpiresAt = null;
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  getAccessTokenExpiresAt(): string | null {
    return this.accessTokenExpiresAt;
  }

  hasAccessToken(): boolean {
    return this.accessToken !== null;
  }
}
