# @absence-management/shared-api-client

The typed API client. `scope:shared`, `type:util`.

`src/generated/` is produced by `@hey-api/openapi-ts` from the OpenAPI document under
`frontend/openapi/` and is checked in. **Never edit it** — run `dotnet build` in the repository
root, then `pnpm gen:api`.

The only hand-written code is `src/lib/`: `ApiError`, the single error type the frontend catches,
and the interceptor that turns the backend's RFC 9457 problem details into one.
