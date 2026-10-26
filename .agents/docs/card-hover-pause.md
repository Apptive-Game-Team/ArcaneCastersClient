# Card hover and practice pause

Before changing in-game card hover or pausing gameplay, check both the client and
the game server. Practice matches run on the server; `Time.timeScale = 0` only
freezes client presentation.

`CardUIZoom` owns the current hover, sends `cardHover` every 0.5 seconds, and
applies the server's `practicePaused` confirmation. It restores the previous
time scale on hide, focus loss, disable and results. Keep hover refreshes,
`PingSender`, and `BarController` on unscaled time. `PingSender` previously used
`Time.deltaTime`, so freezing the client would also stop its 3-second connection
keepalive and make a player reading a card look disconnected.

Keep `GameCardDetails.prefab` non-interactive: its CanvasGroup must have
`blocksRaycasts = false` and every Graphic must have `raycastTarget = false`.
Otherwise the popup can replace the card under the pointer and repeatedly
trigger pointer exit/enter. Use `MagicBookDetailText` for the localized body,
and invalidate pending async content when the popup hides or switches cards.

On the server, paused ticks still drain the action queue, expire hover leases,
and send newly built sync snapshots. Simulation frame numbers stay frozen, so
the normal frame-number sync interval cannot provide the paused heartbeat.
Reusing the previous DTO would repeat projectiles/events; rebuilding a DTO must
also skip mana charging and card drawing in `FrameDataSystem.earlyUpdate`.
