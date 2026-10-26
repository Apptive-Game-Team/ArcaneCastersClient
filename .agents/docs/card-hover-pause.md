# Card hover and practice pause

Before changing in-game card hover or pausing gameplay, check both the client and
the game server. Practice matches run on the server; `Time.timeScale = 0` only
freezes client presentation.

`CardUIZoom` waits one unscaled second on the same card before opening the
GameScene description (`detailPrefab`). Pending cards do not send `cardHover`
or accept pause confirmations. Only a displayed hover sets `hasHover` and sends
`cardHover` every 0.5 seconds. Switching cards, leaving, losing focus, disabling
or removing the card cancels the pending delay. Keep the tutorial's legacy
zoomed card immediate when `detailPrefab` is absent.

`CardUIZoom` owns the current hover and
applies the server's `practicePaused` confirmation. It restores the previous
time scale on hide, focus loss, disable and results. Keep hover refreshes,
`PingSender`, and `BarController` on unscaled time. `PingSender` previously used
`Time.deltaTime`, so freezing the client would also stop its 3-second connection
keepalive and make a player reading a card look disconnected.

Keep `GameCardDetails.prefab` non-interactive: its CanvasGroup must have
`blocksRaycasts = false` and every Graphic must have `raycastTarget = false`.
Otherwise the popup can replace the card under the pointer and repeatedly
trigger pointer exit/enter. Use `MagicBook`'s `summary.<snake_case serverName>`
for the in-game body, with no parameter/stat block. `magic.text` can be an alias,
so do not use `textLocalizationKey` for summaries. Author both locales for every
magic; keep `MagicBookDetailText` for the book and deck's full descriptions.
Missing summaries produce an empty body rather than a raw key or stat fallback.
Invalidate pending async content when the popup hides or switches cards.
Update both `GameCardDetails.prefab`'s initial size and `ZoomedCardUI`'s runtime
height limits when resizing the short description.

On the server, paused ticks still drain the action queue, expire hover leases,
and send newly built sync snapshots. Simulation frame numbers stay frozen, so
the normal frame-number sync interval cannot provide the paused heartbeat.
Reusing the previous DTO would repeat projectiles/events; rebuilding a DTO must
also skip mana charging and card drawing in `FrameDataSystem.earlyUpdate`.
