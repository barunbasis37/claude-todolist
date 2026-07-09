---
name: commit-and-push
description: This skill should be used when the user asks to "commit this", "commit these changes", "commit and push", "create a commit", "push to GitHub", "push this up", or runs "/commit-and-push". Writes a Conventional Commits-formatted commit message for the currently staged changes, commits them, then asks for confirmation before pushing to the remote.
---

# Commit and Push

Turn the currently **staged** changes into a single [Conventional Commits](https://www.conventionalcommits.org/)
commit, then push — pausing for explicit confirmation before the push, since
pushing affects the shared remote.

Only ever act on changes the user has already staged. Never stage files
yourself (no `git add`) — that decision belongs to the user.

## Step 1 — Verify there's a git repo

Check whether `.git` exists at (or above) the current directory (`git rev-parse
--show-toplevel`). If it doesn't:
- Stop. Tell the user this directory isn't a git repository yet and give them
  the exact commands to fix it (`git init`, then add a remote with
  `git remote add origin <url>`). Do not run `git init` yourself — repo
  creation and remote setup are the user's call, not something to do as a
  side effect of "commit and push".

## Step 2 — Check what's staged

Run `git diff --cached --name-status` (and `git status` for full context).

- If nothing is staged: stop and tell the user there's nothing to commit. If
  there ARE unstaged or untracked changes, mention them and suggest `git add
  <files>`, but do not stage anything yourself.
- If something is staged: proceed. Also run `git diff --cached` to read the
  actual content of the staged changes — the commit message must be grounded
  in what the diff really contains, not guessed from file names alone.

## Step 3 — Determine the Conventional Commit type

Infer the type from the staged diff's content, using this priority when a
change could fit more than one (pick the most significant):

| Type | When |
|---|---|
| `feat` | New user-facing functionality (new page/endpoint/handler/public method) |
| `fix` | Bug fix — corrects incorrect behavior |
| `docs` | Documentation-only changes (README, CLAUDE.md, comments, `*.md`) |
| `style` | Formatting/whitespace only, no logic change |
| `refactor` | Code restructuring with no behavior change and not a pure style change |
| `perf` | Performance improvement |
| `test` | Adding or updating tests |
| `build` | Build system or dependency changes (`*.csproj`, lockfiles, package manifests) |
| `ci` | CI/CD configuration changes |
| `chore` | Tooling, config, `.gitignore`, maintenance not covered above |
| `revert` | Reverts a previous commit |

If the staged diff clearly mixes unrelated types (e.g. a new feature plus
unrelated doc cleanup), don't silently smash them together: tell the user what
you see and ask whether to (a) commit as one commit using the dominant type
with the rest noted in the body, or (b) split into multiple commits (in which
case they'll need to re-stage between commits — offer to drive that if they
want). Default to (a) if they don't have a preference.

## Step 4 — Determine scope (optional) and write the message

- **Scope**: a short lowercase token for the area touched, derived from the
  top-level folder/module in the diff (e.g. `pages`, `models`, `data`,
  `docs`, `skill`, `config`). Omit the scope entirely if changes span many
  unrelated areas — don't force one.
- **Subject**: imperative mood, no trailing period, ideally ≤ 72 characters:
  `type(scope): subject` or `type: subject` if no scope.
- **Body** (optional, blank line after subject): explain *what* and *why* for
  anything non-obvious from the diff alone; wrap prose around 72 columns;
  bullet points are fine for multiple distinct changes within the commit.
- **Breaking changes**: if the diff removes/changes a public API, route, or
  schema in a backward-incompatible way, add `!` after the type/scope
  (`feat(api)!: ...`) and a footer paragraph starting with `BREAKING CHANGE:`
  describing the impact.

Write the full composed message (subject + blank line + body/footer) to a
temporary file outside the repo (use the scratchpad directory if available)
rather than trying to inline it as a shell argument — this avoids quoting
issues with special characters and keeps multi-line bodies intact.

## Step 5 — Commit

Run `git commit -F <path-to-temp-message-file>`. Do not pass `--no-verify` —
if a commit hook fails, surface the failure and let the user decide how to
proceed rather than bypassing it.

After committing, show the user the result: `git log -1 --stat` (or
equivalent) so they can see the commit hash, message, and files included.

## Step 6 — Confirm before pushing

Before running `git push`, gather and show the user:
- The current branch (`git rev-parse --abbrev-ref HEAD`).
- The remote it would push to (`git remote -v`) and whether an upstream is
  already tracked (`git rev-parse --abbrev-ref --symbolic-full-name @{u}` —
  note if this fails, meaning no upstream is set yet).
- The commit(s) about to be pushed (`git log @{u}..HEAD --oneline` if
  upstream exists, otherwise just the commit(s) just created).

Then explicitly ask the user to confirm before pushing (e.g. via
AskUserQuestion or a direct question) — do not push automatically as a
continuation of committing. Wait for an affirmative response.

## Step 7 — Push

On confirmation:
- If an upstream is already tracked: `git push`.
- If no upstream is tracked yet: confirm the intended remote/branch with the
  user, then `git push -u <remote> <branch>`.
- Never force-push (`--force`/`-f`) or skip hooks as part of this skill. If
  the push is rejected (e.g. non-fast-forward because the remote has commits
  you don't have), stop and report this to the user with the suggested next
  step (`git pull --rebase` then re-run push) rather than forcing anything.

Report the final result: success with the pushed commit range, or the exact
error if it failed.

## Constraints

- Never stage or unstage files on the user's behalf.
- Never run `git init`, add a remote, force-push, or skip hooks without the
  user explicitly asking for that specific action.
- Never push without an explicit confirmation step in the same run.
- Ground the commit message in the actual staged diff content, not assumptions
  from file names alone.
