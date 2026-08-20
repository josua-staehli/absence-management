import {
  type AbsenceRequestDto,
  approveAbsenceRequest,
  createAbsenceRequest,
  type CreateAbsenceRequestRequest,
  listAbsenceRequests,
  rejectAbsenceRequest,
  updateAbsenceRequest,
  type UpdateAbsenceRequestRequest,
} from '@absence-management/shared-api-client';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

/**
 * Everything the absences area does over HTTP. Requests live in data-access libraries only: a
 * page calls a hook, and the generated client is the single place that knows the routes.
 *
 * All five hooks work on the same list, so every mutation invalidates the same key and the table
 * shows the new state without the pages having to refetch anything themselves.
 */

export const absenceRequestsQueryKey = ['absence-requests'] as const;

export function useAbsenceRequests() {
  return useQuery({
    queryKey: absenceRequestsQueryKey,
    queryFn: async (): Promise<AbsenceRequestDto[]> => {
      // The client is configured with throwOnError, so a failed request rejects here.
      const { data } = await listAbsenceRequests();
      return data;
    },
  });
}

/** The editable fields plus the id of the request they belong to. */
export type UpdateAbsenceRequestVariables = UpdateAbsenceRequestRequest & { id: string };

function useAbsenceRequestMutation<TVariables>(
  mutationFn: (variables: TVariables) => Promise<unknown>,
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn,
    // onSettled, not onSuccess: a refused decision usually means somebody else changed the
    // request, so the list is refetched then too and shows what it really looks like now.
    onSettled: () => queryClient.invalidateQueries({ queryKey: absenceRequestsQueryKey }),
  });
}

export function useCreateAbsenceRequest() {
  return useAbsenceRequestMutation((body: CreateAbsenceRequestRequest) =>
    createAbsenceRequest({ body }),
  );
}

export function useUpdateAbsenceRequest() {
  return useAbsenceRequestMutation(({ id, ...body }: UpdateAbsenceRequestVariables) =>
    updateAbsenceRequest({ path: { id }, body }),
  );
}

export function useApproveAbsenceRequest() {
  return useAbsenceRequestMutation((id: string) => approveAbsenceRequest({ path: { id } }));
}

export function useRejectAbsenceRequest() {
  return useAbsenceRequestMutation((id: string) => rejectAbsenceRequest({ path: { id } }));
}
