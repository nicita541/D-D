# Shared Backend Code

`Shared` is reserved for cross-module primitives that are not owned by one RPG feature module:

- common result and error types;
- JSON helpers;
- security helpers;
- validation helpers;
- database helpers.

Existing shared types will be moved here gradually to keep the refactor behavior-preserving.
