export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, readonly string[]>;
  traceId?: string;
}

export interface ApiError {
  readonly status: number;
  readonly title: string;
  readonly detail: string;
  readonly traceId?: string;
  readonly validationErrors?: Record<string, readonly string[]>;
}

export function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null;
}

export function toApiError(status: number, body: unknown, fallbackTitle: string): ApiError {
  if (isProblemDetails(body)) {
    return {
      status,
      title: body.title ?? fallbackTitle,
      detail: body.detail ?? fallbackTitle,
      traceId: body.traceId,
      validationErrors: body.errors,
    };
  }

  return {
    status,
    title: fallbackTitle,
    detail: fallbackTitle,
  };
}

export function apiErrorToMessage(error: ApiError, fallback = 'Something went wrong.'): string {
  if (error.validationErrors) {
    const firstField = Object.values(error.validationErrors)[0]?.[0];
    if (firstField) {
      return firstField;
    }
  }

  return error.detail || error.title || fallback;
}
