---
name: document-project
description: This skill should be used when the user asks to "document this project", "update the docs", "generate a README", "create/update CLAUDE.md", "sync the documentation", "refresh the project docs", says the docs are stale or out of date, or runs "/document-project" in an ASP.NET Core / .NET web project. Scans the current source tree (csproj, Program.cs, Models, Data/DbContext, Pages or Controllers, launchSettings.json, appsettings.json) and creates README.md and CLAUDE.md at the project root if they don't exist, or reconciles their generated sections with the code's current state if they do, while preserving any hand-written content.
---

# Document Project

Keep `README.md` (human-facing) and `CLAUDE.md` (agent-facing) at the project root
in sync with the actual current state of the source tree. Work by re-scanning the
code every time this skill runs — never rely on git history or diffs, since the
target project may or may not be a git repository at any given time.

This skill targets ASP.NET Core / .NET web projects (Razor Pages or MVC, EF Core
data access), but the scanning procedure is written generically — describe *how to
derive* structure, not a fixed file list — so it keeps working as a project's
files change or grow.

## Step 1 — Locate targets and detect state

1. Confirm the project root: the directory containing the `*.csproj` file (glob
   for it; don't assume a specific name).
2. Check whether `README.md` and `CLAUDE.md` exist at that root.
3. For each that exists, read it fully and check for marker comments of the form
   `<!-- document-project:section:<name>:start -->` / `...:end -->`. These
   delimit auto-generated content. Everything outside any marker pair, and
   everything after the trailing `<!-- document-project:end-of-generated-content -->`
   marker, is human content — never alter, reformat, or reflow it.
4. If a file exists but contains **no** `document-project` markers at all
   (pre-dates this skill), treat it as legacy/hand-written: do not rewrite it.
   In Step 4/5, append one new marked block at the end instead of touching
   existing prose. On the next run, that block will contain markers and update
   normally.

## Step 2 — Scan the source tree

Re-derive everything below from the live tree on every run; do not assume a
previous run's findings are still accurate.

- **Project file**: glob `*.csproj` at the root. Read `<TargetFramework>`, the
  SDK attribute (e.g. `Microsoft.NET.Sdk.Web`), and every `<PackageReference>`
  (name + version). This drives the tech-stack section.
- **Composition root**: find `Program.cs` (or `Startup.cs` if present). Grep for
  `builder.Services.Add*(`, `app.Use*(`, and `app.Map*(` calls to list
  registered services and middleware pipeline order — whatever is actually
  registered, not a fixed assumed set.
- **Data layer**: search all `*.cs` files (excluding `bin/`, `obj/`) for classes
  declared `: DbContext`. For each, list its `DbSet<T>` properties as entities.
  Note whether schema is created via `EnsureCreated()`, `Migrate()`, or neither
  (grep `Program.cs`/the DbContext for these calls) — this matters a lot for the
  `architecture`/`gotchas` sections in CLAUDE.md.
- **Domain models**: for each entity type found above, read its class file and
  list public properties with CLR types, data-annotation attributes
  (`[Required]`, `[StringLength]`, etc.), and any computed defaults.
- **Pages/Controllers**: recursively enumerate everything under `Pages/` (or
  `Controllers/`/`Views/`/`Areas/` if the project uses MVC instead). For Razor
  Pages, derive the route from the file path relative to `Pages/` per file-based
  routing conventions (don't hardcode `/` for an index page — derive it). For
  each `.cshtml.cs` code-behind, grep handler methods matching
  `On(Get|Post|Put|Delete)\w*` and any `[BindProperty]` members. For MVC
  controllers, list action methods and their route attributes instead.
- **Run configuration**: read `Properties/launchSettings.json` — list each
  profile name, its `applicationUrl`(s), and any `environmentVariables`.
- **App configuration**: read `appsettings.json` and any
  `appsettings.<Environment>.json` for `ConnectionStrings` keys and notable
  top-level config sections. Never print values that look like real secrets or
  credentials — name the key, not a live secret value.
- **Static assets**: glob top-level directory names under `wwwroot/lib/` only
  (not individual vendored files) to name front-end libraries in use.
- **VCS state**: check live whether a `.git` directory exists at the root.
  Reflect the current answer each run — don't assume a prior answer still holds.
- **New patterns**: if the scan finds a directory/pattern not covered above
  (e.g. `Services/`, `Repositories/`, `Areas/`, a test project), add a
  corresponding doc subsection only when such files are actually found — don't
  pre-create empty sections for things that don't exist.

## Step 3 — Section marker convention

Wrap every generated section in both files like this:

```
<!-- document-project:section:<section-id>:start -->
... generated content for this section ...
<!-- document-project:section:<section-id>:end -->
```

End every generated file with:

```
<!-- document-project:end-of-generated-content -->
```

Rules:
- Only content **inside** a marker pair is ever regenerated — overwrite the
  inner content wholesale from the fresh scan each run; no need to diff against
  the old inner content first.
- Content outside any marker pair, or below the trailing sentinel, is permanent
  human territory.
- If a section's underlying signal disappears from the code (e.g. all pages
  were deleted), replace that section's body with a short explicit note (e.g.
  "No pages found in this project.") rather than deleting the section entirely.
- Add a single `<!-- document-project: last synced <YYYY-MM-DD> -->` comment
  near the top of each generated file, updated every run, for lightweight
  traceability.

## Step 4 — Generate or reconcile README.md (human audience)

Target sections, each its own marker pair:

- `header` — project title + one/two sentence purpose statement.
- `tech-stack` — target framework, key NuGet packages, DB engine, UI libs found.
- `prerequisites` — SDK version required, derived from `<TargetFramework>`.
- `getting-started` — `dotnet restore` / `dotnet run` steps, the actual URLs
  from `launchSettings.json`, and (if found) a note that the DB is created
  automatically with no migration step required — or migration instructions if
  `Migrate()` is used instead.
- `project-structure` — a directory tree (top ~2 levels, excluding
  `bin/`/`obj/`/`.git/`/`wwwroot/lib/` internals) with a one-line purpose per
  folder inferred from its contents.
- `data-model` — one subsection/table per entity: field name, type, constraints.
- `pages-and-routes` — table of route, page/controller file, HTTP handlers,
  one-line purpose, generated from the Pages/Controllers scan.
- `configuration` — where the connection string / key config lives and how to
  change it.

If generating for the first time, write the full file with these markers
already in place. If reconciling, replace only the inner content of each
existing marker pair; add any marker pairs that are new (e.g. a
`pages-and-routes` section didn't exist before) in a sensible position before
the trailing end marker.

## Step 5 — Generate or reconcile CLAUDE.md (agent audience)

Do **not** duplicate README's full tables — reference them instead. Target
sections:

- `overview` — 1–2 lines: what the app is, tech stack in one line, "see
  README.md for full setup/data-model/routes detail".
- `commands` — copy-pasteable build/run commands and the real ports from
  `launchSettings.json`. State plainly if no test project/commands exist yet.
- `architecture` — call out conventions that are non-obvious or non-default,
  derived from what was actually found in Step 2: e.g. minimal hosting model,
  file-based routing, single DbContext via DI, and — if the scan found
  `EnsureCreated()` rather than `Migrate()` — an explicit warning that schema
  changes won't apply to an existing local DB file and migrations are not in
  use, so an agent shouldn't reflexively run `dotnet ef migrations add` without
  first checking how the schema is actually created.
- `key-files` — table of the composition root, DbContext(s), each domain model,
  and the primary functional pages/controllers, each with a one-line
  "why it matters".
- `extending-the-app` — a short checklist for adding a new page/entity that
  mirrors the existing pattern found in the scan (e.g. "add a POCO under
  Models/, add a DbSet<T> to the DbContext, add a Pages/<Name>.cshtml +
  .cshtml.cs with OnGet/OnPost handlers, link it from the shared layout if it
  needs nav").
- `gotchas` — bullet list built from live checks each run: whether a test
  project exists, whether `.git` exists, anything notably missing that a future
  agent might otherwise wrongly assume is present (e.g. no CI config found).

## Step 6 — Report what changed

After writing, print a concise summary grouped by file, listing only the
sections that actually changed content (skip unchanged sections):

```
Updated README.md:
  - <section>: <one-line description of what changed>
Updated CLAUDE.md:
  - <section>: <one-line description of what changed>
```

If a file was created for the first time, say so instead of listing section
diffs ("Created README.md and CLAUDE.md — first run"). If nothing changed, say
so explicitly rather than printing an empty report. Do not create or maintain a
separate `CHANGELOG.md` file — this summary is conversational/transient only.

## Constraints

- Never touch content outside recognized `document-project` markers.
- Never invent sections unsupported by something actually found in the scan
  (e.g. don't add a "Contributing"/"License" section unless asked).
- Always re-scan live rather than trusting the existing doc's claims about the
  code — the doc documents the code, not the other way around.
- Do not depend on `.git` being present; if present, it's fine to note facts
  opportunistically (e.g. current branch), but never require it.
