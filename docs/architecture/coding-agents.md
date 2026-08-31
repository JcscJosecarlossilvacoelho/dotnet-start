---
title: Working with coding agents
description: Shaping a .NET repository so Claude, Codex, and their successors produce code you would have written.
order: 50
---

An agent reads your repository the way a new colleague does, with less context and more speed. Everything that helps a newcomer helps an agent, only more so.

## Make the repository legible

- **One README that runs.** The commands to restore, run, and test must work verbatim on a clean clone.
- **`AGENTS.md` / `CLAUDE.md`** at the root: the stack, the conventions, the commands, and the things not to do. Keep it short enough to stay true.
- **Consistent structure.** If orders live in `Features/Orders`, payments live in `Features/Payments`. Predictability is the whole game — see [Structuring an application](/docs/architecture/project-structure).
- **A current [OpenAPI document](/docs/web/openapi)** — the fastest way to hand over an accurate picture of your API.

## Make verification cheap

An agent's output is only as safe as your ability to check it.

```bash
dotnet build   -warnaserror
dotnet test
dotnet format  --verify-no-changes
```

Three commands, no manual steps, no hidden environment. `TreatWarningsAsErrors`, analysers, and [integration tests](/docs/testing/integration-testing) are what let you accept a change on evidence rather than on vibes.

## Write prompts that carry constraints

A weak prompt describes a feature. A useful one describes the feature, the constraints, and the proof:

> Add `POST /v1/orders/{id}/cancel` to `src/MyApp.Api`. Follow the existing slice pattern in `Features/Orders`. Cancelling is only valid from `Placed`; return 409 otherwise, as `ProblemDetails`. Add integration tests in `MyApp.IntegrationTests` covering success, wrong-state, and unauthorised. Run `dotnet test` and show me the diff.

State the file, the pattern to follow, the rule, the error contract, the tests, and the command that proves it.

## Review what agents get wrong

Read every diff with these in mind — they are the recurring failure modes in .NET code:

- A scoped service (like `DbContext`) injected into a singleton — see [dependency injection](/docs/web/dependency-injection).
- `.Result` or `.Wait()` reintroduced somewhere — see [async](/docs/csharp/async-await).
- Missing `CancellationToken` propagation.
- N+1 queries hidden in a loop — see [querying](/docs/data/querying).
- Silent `catch` blocks and swallowed exceptions.
- Authorization applied to the happy path but not to a new endpoint.
- A test that asserts the implementation rather than the behaviour.

## Keep humans in the loop where it counts

Let agents do the work that is verifiable: tests, refactors, migrations, boilerplate, documentation. Keep the irreversible decisions — schema shape, public contracts, security boundaries — as things a person approves. That division is what makes the speed safe.

## Further reading

- [Contribute a skill to this site](https://github.com/JcscJosecarlossilvacoelho/dotnet-start/tree/main/skills)
