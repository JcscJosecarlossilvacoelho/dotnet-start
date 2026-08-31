# Contributing to the documentation

Everything under `/docs` on the site is a Markdown file in this folder. There is no
CMS, no database, and no component to edit: **adding a file publishes a page.** This
guide explains the rules the loader enforces so your page renders the way you expect.

For the project's writing principles, see [CONTRIBUTING.md](../CONTRIBUTING.md).

## Layout

```
docs/
  README.md              <- this file, never published
  start/
    _section.md          <- describes the section, never published
    what-is-dotnet.md    <- published at /docs/start/what-is-dotnet
    getting-started.md
  web/
    _section.md
    routing.md           <- published at /docs/web/routing
```

Two rules follow from how the loader walks this folder:

- **Pages must live in a section folder.** A `.md` file at the root of `docs/` is
  ignored — which is why this README is safe here.
- **The URL is the path.** `docs/<section>/<page>.md` is served at
  `/docs/<section>/<page>`. Renaming a file breaks its links, so treat filenames as
  permanent once merged.

Folders beginning with `.` or `_` are skipped entirely, as is any section that
contains no pages.

## Front matter

Every page starts with a `---` fenced block of `key: value` lines:

```markdown
---
title: Routing
description: How requests reach an endpoint, and how to shape the URLs you expose.
order: 30
---
```

| Key | Required | Effect |
| --- | --- | --- |
| `title` | Yes in practice | The `<h1>`, the sidebar entry, the browser tab, and the strongest search signal. Falls back to a humanised filename. |
| `description` | Yes in practice | The sub-heading, the index card blurb, and the second search signal. Defaults to empty. |
| `order` | No | Sort position within the section, ascending. Defaults to `500`; ties break alphabetically by title. |

The parser is deliberately small: one `key: value` per line, no nesting, no lists.
Surrounding quotes are stripped, so `title: "Routing"` and `title: Routing` are the
same. A malformed block is treated as body text, which is the usual reason a page
appears with a filename-shaped title.

Leave gaps in `order` — `10, 20, 30` rather than `1, 2, 3` — so a page can be
inserted later without renumbering the section.

## Sections

A section is a folder. Describe it with `_section.md`, which uses the same three
keys and has no body:

```markdown
---
title: Getting started
description: From an empty folder to a running application.
order: 10
---
```

Without a `_section.md` the folder still works: the title is humanised from the
folder name and it sorts to the end.

## How a page is rendered

- **Drop the `# Title` heading.** The page chrome already prints the title from your
  front matter, so a leading H1 in the body is stripped to avoid printing it twice.
- **`##` headings build the "on this page" rail.** Only level-2 headings appear.
  Their anchors are generated GitHub-style, so `## Model binding` becomes
  `#model-binding` — the same anchor GitHub renders, which means links copied from a
  pull request preview keep working.
- **Line breaks are literal.** A single newline renders as a `<br>`, so wrap
  paragraphs deliberately rather than at an arbitrary column.
- Tables, footnotes, task lists, and fenced code with language hints are all
  available (Markdig's advanced extensions are enabled).

## Linking

Link between pages with site-relative paths:

```markdown
See [routing](/docs/web/routing) for how the request reaches your handler.
```

Not `../web/routing.md` — relative file links break, because the served URL has no
`.md` and no matching folder depth.

## Search

The ⌘K palette matches **every** word of the query, and ranks a page higher when the
words appear in its title, description, or slug rather than only in the body. Two
consequences worth writing for:

- A precise `title` and `description` are the whole ranking signal. "Routing" beats
  "How it all fits together".
- Include the words a reader would actually type, including the ones you would
  otherwise avoid repeating — the term someone searches for is rarely the elegant
  synonym.

## Preview your change

```bash
dotnet run --project dotnet-start.csproj
```

Then open <http://localhost:5000/docs>. In Development the library re-reads this
folder about once a second, so saving a file and refreshing is enough — no restart.
In Production the content is read once at startup and never re-scanned.

## Checklist before opening a pull request

- [ ] The file is in a section folder, with front matter containing `title` and `description`.
- [ ] No `# Heading` at the top of the body.
- [ ] Section headings are `##`, and read sensibly as a standalone table of contents.
- [ ] Commands and code run as written, on a clean machine.
- [ ] Links to other pages use `/docs/...` paths.
- [ ] The page states one outcome the reader can verify, and a clear next step.
- [ ] You previewed it locally and the sidebar, TOC, and search all show it correctly.
