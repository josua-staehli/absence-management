# @absence-management/absences-data-access

Every HTTP request of the absences area: the list query and the four mutations, as TanStack Query
hooks over the generated client. `scope:absences`, `type:data-access` — pages call these hooks, and
nothing outside this library knows the routes.

It also re-exports `AbsenceRequestDto`, `AbsenceType` and `AbsenceStatus`, so a feature library
depends on this area instead of on `shared/api-client`.
