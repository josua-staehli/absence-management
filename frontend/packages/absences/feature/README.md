# @absence-management/absences-feature

The pages of the absences area: the overview with the table and the decisions, and the one form
that both creates and edits a request. `scope:absences`, `type:feature`.

`AbsencesPage` takes one boolean prop per action, which is how the two applications differ: the
employee application passes `canAddRequest` and `canEditRequest`, the approver application passes
`canDecideRequest`.
