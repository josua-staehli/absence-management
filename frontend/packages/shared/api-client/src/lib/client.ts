import { i18next } from '@absence-management/shared-i18n';

import { client } from '../generated/client.gen';
import type { ProblemDetails } from '../generated/types.gen';
import { ApiError } from './api-error';

/**
 * Turns everything the fetch client can reject with into an {@link ApiError}.
 *
 * The backend answers a failed request with RFC 9457 problem details (see the C# side in
 * `Common.Api/ResultExtensions.cs`), and the generated client rejects with the parsed body - a
 * bare object with no prototype and no message. Mapping it here is what lets every page show the
 * business message the domain wrote instead of "[object Object]".
 */

/** `code` is an RFC 9457 extension member, so the generated type does not have it. */
interface ProblemDetailsWithCode extends ProblemDetails {
  code?: string;
}

client.interceptors.error.use((error, response): ApiError => {
  const problem = error as ProblemDetailsWithCode | null;
  const detail = typeof problem?.detail === 'string' ? problem.detail : undefined;

  // The fallback covers a request that never reached the API, or an answer that is not a problem
  // details document. The message of the backend is passed through as it is - it is the one
  // piece of text in the frontend that is not translated here.
  return new ApiError(detail ?? i18next.t('errors.unexpected'), response?.status, problem?.code);
});

/**
 * The configured client. Re-exported so that importing this module - and with it registering the
 * interceptor above - is an ordinary import instead of a side-effect-only one.
 */
export { client };
