/**
 * The single error type the frontend has to know about. Every failed request arrives as one of
 * these, so a component can render `error.message` without asking where the failure came from.
 *
 * `code` is the stable identifier of the backend's business error (e.g. `Absences.Overlapping`).
 * Nothing branches on it today - it is what a message catalogue would key on later.
 */
export class ApiError extends Error {
  readonly status: number | undefined;
  readonly code: string | undefined;

  constructor(message: string, status?: number, code?: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
  }
}
