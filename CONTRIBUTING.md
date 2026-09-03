# Contributing to .NET Start

Thanks for helping make .NET easier to start with.

## Principles

- Prefer one clear recommendation over a catalogue of alternatives.
- Write for a developer building their first real .NET application.
- Keep examples current, runnable, and small.
- Explain the reason behind a choice without turning a guide into a course.

## Documentation

Documentation lives in `docs/<section>/<page>.md`. [docs/README.md](docs/README.md) is the full reference for front matter, sections, anchors, linking, and local preview. Fork the repository, create a focused branch, edit a Markdown file, and open a pull request explaining what became clearer. Adding a file publishes a page; no C# change is needed.

Front matter is required:

```markdown
---
title: Short page title
description: One sentence describing what the page covers.
order: 30
---
```

To add a whole section, create a folder with a `_section.md` holding the same three fields.

Reference pages explain how something works and why you would choose it; keep step-by-step tutorials in `start/`. Link between pages with site-relative links (`/docs/web/routing`) so the connections survive renames of the site chrome.

Small improvements are welcome: typos, clearer examples, better defaults, and missing steps all matter.

Every practical guide should contain:

- one outcome the reader can verify;
- commands or code that can be run as written;
- a short Claude/Codex prompt that reaches the same outcome;
- a clear next step.

## Tests

C# behaviour lives in `DotnetStart.Tests`. After changing `Content/DocsLibrary.cs`, forwarded-header setup, or a Razor page that sets an HTTP status, run:

```bash
dotnet test
```

## Skills

Installable agent skills live in `skills/<skill-name>/SKILL.md`. Keep each skill focused on decisions that materially improve an agent's work. Validate new or changed skills before opening a pull request.
