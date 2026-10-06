# S4: Online test with two players

The goal of this step (the "networking spike") is to prove two people can run the course together, see each other, catch fish and throw them. It is built on **Netcode for GameObjects**.

## 1. Install the packages (once)

In Unity: **Window > Package Manager**, click the **+** at the top left, choose **Install package by name**, and install both of these (one at a time):

1. `com.unity.netcode.gameobjects` (the networking itself)
2. `com.unity.multiplayer.playmode` (lets you run a second player inside the editor, so you can test alone)

Until the first one is installed the networking scripts are ignored on purpose, so the rest of the project compiles as normal. After it installs, Unity recompiles and the menu item below appears.

## 2. Build the scene

Menu **Badeland > Create S4 Network Test Scene**. It builds the same floating course as S3, but with no ready-made player. Instead:

| Object | What it does |
|---|---|
| `NetworkManager` | Connects players and creates one player per connection. |
| `NetworkPlayer` prefab (`Assets/_Project/Prefabs/Characters`) | The normal player plus the network scripts. |
| `FishNetwork` | The list of fish species, so a species can be sent over the network. The catch and throw messages run through the players. |
| `NetworkMenu` | On-screen Host and Join buttons at the top right. |

## 3. Test with two players on one computer

1. Open **Window > Multiplayer > Multiplayer Play Mode** and turn on **Player 2**. A second small Game window appears.
2. Press **Play**.
3. In the main window click **Host**.
4. In the Player 2 window click **Join** (the address `127.0.0.1` is right for the same computer).
5. You should see two coloured capsules. Each window controls its own player: click in a window to give it your keyboard.

If the Multiplayer Play Mode window does not look like this, tell me what you see. It changes between Unity versions.

## 4. Test with a friend (later)

Host clicks **Host**. The friend types the host's IP address (same home network) and clicks **Join**. Over the internet this needs port forwarding or a relay (Steam relay is planned), so start on the same network.

## What to test

- Do both players see each other move smoothly?
- Do both see the same fish at the same time? Does only one player get a fish when both run at it?
- Catch a fish, face the other player and press **F** (throw). Does the other player get it, with its effect?
- Is the rotating bar in the same place on both screens? Does it knock you only when it really hits you?
- Run a lap on both. Do both lap counters work?
- Disconnect and rejoin. Does it recover?

## How it works (short)

- **Movement:** each player's own computer moves their own character and publishes its position. Other computers see a smoothed copy. Simple and instant to control, but it trusts every player, which is fine for co-op with friends.
- **Shared clock:** fish leaps and the rotating bar depend on one network clock, so everyone sees the same thing.
- **Fish catch:** a player asks the host, and the host accepts the first request for each fish.
- **Throw:** the thrower asks the host, who checks the target is empty-handed and forwards the fish to the target's computer.

## Known limits (on purpose, for the spike)

- Lap counting runs on every computer from the shared positions. The monster trigger will need a single host decision.
- Other players' held fish is shown only in their mirrored state, with no visible icon yet.
- Falling or knockback effects are decided on the affected player's own computer.
- No lobby, names, reconnection handling or Steam yet.
