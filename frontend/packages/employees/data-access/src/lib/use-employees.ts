import { type EmployeeDto, listEmployees } from '@absence-management/shared-api-client';
import { useQuery } from '@tanstack/react-query';

/**
 * The employee list. Requests live in data-access libraries only: a feature calls the hook, and
 * the generated client is the single place that knows the route and the response type.
 */

export const employeesQueryKey = ['employees'] as const;

export function useEmployees() {
  return useQuery({
    queryKey: employeesQueryKey,
    queryFn: async (): Promise<EmployeeDto[]> => {
      // The client is configured with throwOnError, so a failed request rejects here.
      const { data } = await listEmployees();
      return data;
    },
  });
}
