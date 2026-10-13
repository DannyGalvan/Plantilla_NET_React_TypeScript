import { isAxiosError } from 'axios';

/** RFC 9457 ProblemDetails returned by the API for every 4xx/5xx. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

/** Normalized API error used across the app (forms map `errors` to fields). */
export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly traceId?: string;
  readonly errors: Record<string, string[]>;

  constructor(status: number, code: string, message: string, errors: Record<string, string[]> = {}, traceId?: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
    this.errors = errors;
    this.traceId = traceId;
  }

  static from(error: unknown): ApiError {
    if (error instanceof ApiError) {
      return error;
    }

    if (isAxiosError<ProblemDetails>(error)) {
      if (!error.response) {
        return new ApiError(0, 'network.unavailable', 'The server is not reachable.');
      }

      const problem = error.response.data ?? {};
      return new ApiError(
        error.response.status,
        problem.code ?? `http.${error.response.status}`,
        problem.detail ?? problem.title ?? error.message,
        problem.errors ?? {},
        problem.traceId,
      );
    }

    return new ApiError(0, 'unexpected', error instanceof Error ? error.message : 'Unexpected error');
  }
}

/** First message of every field, for displaying a summary. */
export function firstErrors(error: ApiError): string[] {
  return Object.values(error.errors)
    .map((messages) => messages[0])
    .filter((message): message is string => Boolean(message));
}
