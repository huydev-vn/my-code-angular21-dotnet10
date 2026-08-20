import { HttpErrorResponse } from '@angular/common/http';

import { apiErrorToMessage, toApiError } from './api-error.model';

export function mapHttpError(error: unknown, fallback = 'Something went wrong.'): string {
  if (error instanceof HttpErrorResponse) {
    return apiErrorToMessage(toApiError(error.status, error.error, fallback), fallback);
  }

  if (error instanceof Error && error.message) {
    return error.message;
  }

  if (typeof error === 'string' && error.trim().length > 0) {
    return error;
  }

  return fallback;
}
