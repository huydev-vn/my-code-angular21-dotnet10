import { mapHttpError } from '../../core/http/map-http-error';

export function toErrorMessage(error: unknown, fallback = 'Something went wrong.'): string {
  return mapHttpError(error, fallback);
}
