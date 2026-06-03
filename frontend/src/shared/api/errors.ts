import { ApiError } from './types';

export function getErrorMessage(error: unknown) {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'Произошла неизвестная ошибка.';
}

export function isUnavailableActionError(error: unknown) {
  return error instanceof ApiError && [404, 405, 501].includes(error.status);
}
