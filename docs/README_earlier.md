# Ember — Don't Trust The Torch

Unity 6000.6.5f1 project: **My project**. Current character direction: **32×32 native pixel art**.

- Current art: [design plate](art/ember32/design_plate.png), [animation preview](art/ember32/preview.html), `.pix2` files in each character folder under `art/ember32`.
- Play in Unity: open `Assets/Scenes/Ember.unity`, press Play. If not present, run **Ember → Create or open Ember scene**.
- Windows: `Builds/Windows/Ember.exe`. This is a prototype build, not a finished GDD release.
- Full implementation and remaining work: [status ledger](docs/IMPLEMENTATION_STATUS.md).

Start a solo expedition, or host/join LAN/VPN using the host IP and matching room password. UDP port 27841 must be reachable on the host. Public room-code matchmaking/relay is not implemented yet.

WASD/arrows move; Space/left click attack; C skill; F dash; R indicators; E harvest/interact/extract; B deposit cores at beacon; Q refuel; Tab crafting; 1–4 consumables. M toggles mouse movement, right click sets a path. T toggles the handheld lantern; L gives it to a nearby teammate. ESC opens Sanctuary/menu (host simulation pauses there).

Gather 20 cores, deposit at the beacon, defend for 90 seconds, then interact at the portal. Dead players become wisps; an ally can use a revival stone at the beacon. Stay near light to reduce dread. A mad player outside light can mistake teammates for enemies; the host validates conditional friendly fire.

Only the starting traveler is unlocked in a new profile. Extract to earn currencies, unlock perks and draw vessels/hats/pets. Save data is local and encrypted with integrity checks; it is not an anti-cheat guarantee.
