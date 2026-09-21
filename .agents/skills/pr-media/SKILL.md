---
name: pr-media
description: Put screenshots, renders, or diagrams into a GitHub pull request by committing them to the repository and linking them from the PR body. Use whenever a PR would be easier to review with a picture — scene or UI changes, art replacements, before/after comparisons, layout fixes — or when a reviewer asks to see the change.
allowed-tools:
  - Bash
---

# PR Media

`gh` cannot upload images. The web UI can, but an agent has no way to drive that
drag-and-drop. So the only way to get a picture into a PR from the command line
is to **commit the file and link to it**.

`AGENTS.md` already asks for "screenshots or short clips for UI or scene changes"
in a pull request. This is how that requirement is met.

## Where the files go

```
docs/pr-media/<issue-num>/<name>.png
```

One directory per issue, not per PR — a follow-up PR on the same issue reuses it.
Name files for what they show, not for the order they were made:
`field-16x9.png`, `lobby-before.png`, `lobby-after.png`. Never `1.png`,
`screenshot.png`, `final-final.png`.

## Linking

Link with a raw URL **pinned to the commit SHA**, never to the branch:

```markdown
![필드 16:9](https://raw.githubusercontent.com/Apptive-Game-Team/ArcaneCastersClient/<sha>/docs/pr-media/60/field-16x9.png)
```

A branch-pinned URL breaks the moment the branch is deleted on merge, which
takes every picture in the PR history with it. Commit the images first, read the
SHA back, then write the body:

```bash
git add docs/pr-media/60 && git commit -m "docs: PR 검토용 렌더를 추가한다"
git push
SHA=$(git rev-parse HEAD)
gh pr edit <n> --body "$(...)"    # embed $SHA in the URLs
```

Images in a later commit need a new SHA in the links; edit the body again rather
than leaving a stale one.

## What to commit

Commit the evidence, not the process. One or two pictures that show the change,
plus a before/after pair when the point is a comparison. Every intermediate
iteration belongs in `.art/concept/` or nowhere.

Keep each file **under about 400KB**. These land in the client repository, which
is already heavy with art, and a reviewer loading the PR pays for all of them.

```bash
magick in.png -resize 1280x -strip -colors 200 -define png:compression-level=9 out.png
magick identify out.png
```

`-colors 200` costs nothing visible on flat-shaded game art and typically cuts a
1.3MB render to about 330KB. Photographic or heavily dithered images will band —
check the result before committing it.

## Producing the picture

Whatever the source, the file has to exist on disk before it can be committed.

- **Scene or art changes** — the Editor cannot run in this environment, so a
  screenshot of the running game is not available. Render the scene offline
  instead: parse `GameScene.unity` for the camera, the ground plane and the
  placed sprites, and project them. That reproduces billboard rotation and the
  `CustomAxis (0, 1, 2)` transparency sort faithfully enough to review
  composition, occlusion and colour. Say in the PR that it is an offline render
  and that Editor verification is still outstanding.
- **UI changes** — the same applies; a rendered mock is better than nothing, and
  the PR should say which it is.
- **Never present a render as a screenshot of the running game.** A reviewer who
  believes they are looking at the real thing will not re-check it in the Editor.

## Cleaning up

`docs/pr-media/` is review material, not shipped content. It sits outside
`Assets/` so Unity never imports it and it adds nothing to the build. Leave old
directories in place — they are what makes a merged PR still readable a year
later.
