export * from './lib/use-absence-requests';

// The contract types of this area, re-exported so that a page depends on the absences library
// and not on the generated client.
export {
  type AbsenceRequestDto,
  AbsenceStatus,
  AbsenceType,
} from '@absence-management/shared-api-client';
