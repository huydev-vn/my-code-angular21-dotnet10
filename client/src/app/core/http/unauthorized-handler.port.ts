import { InjectionToken } from '@angular/core';

export const UNAUTHORIZED_HANDLER = new InjectionToken<() => void>('UNAUTHORIZED_HANDLER');
