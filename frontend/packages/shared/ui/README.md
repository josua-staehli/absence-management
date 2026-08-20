# @absence-management/shared-ui

The presentational components both applications share: the two tables, the status badge, the
language switcher and `AppLayout`. `scope:shared`, `type:ui`.

Nothing here makes a request. A component renders what it is handed, and its loading and error
states are props, so a page cannot forget either. The two tables have a Storybook story per state:

```bash
pnpm storybook
```
