# emote icon set — generation log

Subject: five `Assets/Art/Images/Emote/*.png` emoji-style icons for issue #105
(`Laugh`, `Greet`, `Taunt`, `Cry`, `Surprised`). Each sits inside a speech
bubble above a caster at roughly 64px, so the icon has to read at that size —
see `raw/laugh-v1-portrait-reject.png` below for what happens when it doesn't.

References used: `.art/anchors/master-v2/MasterStyleKey.png` (technique) and
`.art/concept/player-restyle/player-D-longhair.png` (hair colour and identity
only — not copied as a composition).

## Laugh, attempt 1 — rejected, wrong subject

Kept locally at `raw/laugh-v1-portrait-reject.png` (1254x1254, not committed —
`raw/` is gitignored, full-size rejects do not ship). Prompt asked for a head/neck/
shoulders bust with a cloak, hood and robe collar, matching the earlier
player-restyle prompts. The rendering technique came back correct — faceted
planes, hard creases, flat colour, no painterly shading — but it is a detailed
character bust: a dozen individual hair-lock planes, a blue hooded cloak, a
grey collar, a belt gem. At the 64px display size the hair and cloak collapse
into noise and the expression, the only thing the sprite exists to convey,
is a few pixels wide. Rejected without shipping; does not count against a
later attempt's numbering beyond attempt 1 of 3.

Also notable: `image_gen` was asked for a flat magenta background (RGB 255 0
255) and ignored it — it returned real alpha instead, corners `(0,0,0,0)`,
subject alpha up to 253 (see `.art/STYLE.md` / the make-game-art skill's
"`image_gen` sometimes returns real alpha anyway" section — already documented,
this run is another data point for it). Read the corner alpha and mode before
running `key-out-background.py` on any candidate in this batch; several may
already have real alpha and keying them would throw the alpha away.

## Redraw brief (attempts 2+, all five)

Rewrote `.art/concept/emote-prompts/*.txt` to an icon brief instead of a
portrait brief:

- Head only. No neck, shoulders, cloak, hood, collar, or gem (`Greet` is the
  one exception — see below).
- Head fills the canvas edge to edge, round or near-round.
- Roughly a dozen large planes for the whole head, not fifty — hair collapses
  to two or three plane masses, not individual locks.
- Face features (eyes, brows, mouth) oversized and high-contrast against the
  skin, the way an emoji reads.
- Keep the faceted low-poly papercraft technique unchanged.
- Keep just enough identity (hair colour, hair mass shape) that the five read
  as one set.

`Greet` needs a visible wave per the issue table, so its prompt keeps the head
as the dominant shape and adds one small hand (four or five flat planes, no
attached arm or shoulder) entering the frame at ear height — everything else
in the icon brief still applies.

Verification before accepting any candidate: downscale to 64x64 and look at
it. If the expression cannot be told apart from the other four at that size,
it is not done regardless of how it looks at 1024.
