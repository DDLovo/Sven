# Changelog

## 0.2.0

- First public release, bringing together the completed Sven gameplay, alternate skills, three relics and two appearances.
- Added an English release page with four gameplay/selection screenshots and separate Thunderstore, GitHub and issue links.
- Set the package website to the Sven GitHub repository.
- Includes all 0.1.15 status, animation compatibility and text improvements. Gameplay and balance are unchanged from 0.1.15.

## 0.1.15

- Echo Sabre cooldown now uses one timed status instead of stacked seconds, removing the redundant xN counter. Its icon is substantially darker.
- Momentum Cleave grants 2s of Black King Bar immunity; God's Strength retains 6s.
- Expanded the bilingual overview with native survivor tip formatting and alternate-skill advice. Momentum Cleave uses the native Heavy keyword. Renamed the cosmetic to Vigil Triumph / 守夜丰功 and added the requested God's Strength flavor text.
- Added a non-rendering native Body animation layer for both skins, preventing missing-state / invalid-layer calls when native movement states resume without interrupting Sven's attack animation.

## 0.1.14

- Increased Storm Hammer enemy contact assistance to a 4m radius, including Aghanim flight. Terrain collision remains unchanged.
- Blink Dagger now plays the native Huntress blink effect at both departure and arrival, with Huntress flash overlays on Sven at the destination.
- Rewrote English and Simplified Chinese survivor/skill descriptions in the style of native survivors. Standardized formal names, including Blink Dagger, Warcry and the Aghanim upgrades, and added the native Stunning keyword tooltip.

## 0.1.13

- Enlarged the Storm Hammer gauntlet by 40% and expanded its surrounding lightning cloud for better readability at range.
- Rebuilt the golden flight wake as spatial streams aligned with the projectile direction, with backward-flowing sparks and a broader lingering trail. Replaced the directional camera-facing glow with a round soft corona.
- Preserved projectile speed, range, collision assistance, impact radius and damage.

## 0.1.12

- Momentum Cleave now deals damage progressively along each rotating blade wave, rather than applying a full-circle hit at the end. Each enemy can be hit only once per turn, with separate hit tracking for the second turn. Total damage, range and cooldown are unchanged.
- Sweeps cover intervening angles on long simulation ticks and stop when interrupted or killed.
- Added cyan, blue and soft violet variation to normal primary cleaves. Reduced filament width by 30% and opacity by 35%, and softened the electrical edge while preserving the main blade.

## 0.1.11

- Momentum Cleave now performs two full circular cuts using mirrored and retargeted Dota Juggernaut Blade Fury motion for both Sven skins.
- Each turn deals 1000% damage plus 30% per m/s, preserving the previous combined damage. The 20m radius and 7s cooldown remain; damage occurs at the end of each turn.
- Added rotating spatial blade waves with a thick curved cross-section and layered interior trails. Empowered cuts blend crimson, orange and gold.
- Corrected the sword roll so the edge leads the spin instead of striking with the broad face.
- Added indexed two-hit synchronization, interruption cancellation and updated skill descriptions.

## 0.1.10

- Melee damage now follows each source slash plane and the same elliptical sector as its visual. Overhead, lateral, diagonal and Echo attacks use their matching profiles.
- Added 2m primary / 3m heavy half-thickness perpendicular to the cut, with actual hurt-collider contact checks for edge hits. Momentum retains its independent 3m rear reach.
- Preserved cast-time damage, coefficients, cooldowns, line-of-sight blocking and one hit per target per strike.

## 0.1.9

- Fixed overhead cleaves flattening into horizontal waves during flight. Each attack now uses a signed slash plane baked from its source animation, retained through travel and fade.
- Removed live-pose sampling delays from launched cleaves, making the correct plane available immediately even at high attack speeds or on Echo follow-ups.
- The leading edge follows the cut and fades first. Both skins, empowered colors, full crosshair pitch, and existing forgiving damage volumes are preserved.

## 0.1.8

- Cleave forms from sampled positions of the actual animated sword before releasing into the aimed sector. Each attack owns its trajectory so Echo, movement and following attacks cannot pull an existing wave around.
- Preserved damage, range and hit timing. Added checks for both skins, all three attacks, low frame rates and fast Echo follow-ups.
- Warcry now layers one of four original Dota battle cries over its existing classic, Immortal or Shard skill sound, avoiding consecutive repeated yells.

## 0.1.7

- Enlarged both Sven skins and the selection display by 30%.
- Cleave now advances and fades progressively along each source swing: overhead, right-to-left, then left-to-right. Echo follows its actual next animation; swing decisions are synchronized between peers.
- Added crimson, orange and gold layers to empowered cleaves.
- Reduced Momentum Cleave cooldown to 7s and extended God's Strength to 10s.
- Enlarged the Storm Hammer fist and its energy envelope by 50%; enemy contact assistance now has a 2m radius, with separate small terrain collision.
- Aghanim's Storm Hammer now deals 300% damage, uses Dota's dedicated secondary skill icon, and surrounds the flying hero with directional pressure waves and electricity. Normal Storm Hammer remains at 600%.

## 0.1.6

- Added a bright ready indicator and a separate muted cooldown icon for Echo Sabre, following the native elemental bands.
- Echo cooldown now displays three expiring stacks (3, 2, 1) before returning to ready. Relic changes and death clear the indicators; combat damage and cooldown duration are unchanged.

## 0.1.5

- Extended Storm Hammer to 80m and Aghanim flight to 40m. Increased both impact radii to 8m.
- Enlarged hammer enemy contact detection, including initial overlaps, while retaining smaller terrain collision and cover checks.
- Extended Great Cleave to 10m, Momentum Cleave to 20m, and Blink Dagger to 30m. Cleave propagation is 35% faster; damage coefficients and cooldowns are unchanged.
- Replaced flail-based airborne animations with authored jump/fall poses, cloth movement and locomotion transitions for both skins.
- Added a distinct overhead Momentum Cleave with a raised anticipation and fast strike, aligned with the existing 0.20s hit time.
- Corrected conflicting Immortal weapon-root bindings and isolated the sword animation layer. Strength recovery and interrupted opening pass controller checks; the reported live-game underground-sword trigger still needs confirmation.
- Assigned distinct Dota icons to alternate skills and transparent subject-only icons to all visible Sven buffs.
- Updated both languages and added collision, animation-transition and capture checks.

## 0.1.4

- Replaced flattened Great Cleave and Storm Hammer impact atlases with spatial effects rebuilt after inspecting original base and TI7 Dota particle playback from multiple angles.
- Cleave now has a thick curved blade, inward electrical streaks and a short residual wave. God's Strength has a saturated red core and golden tips.
- Storm Hammer now bursts in three dimensions with an expanding spherical front, rising plasma coils, electrical clouds and ballistic sparks.
- Melee attacks follow crosshair pitch instead of flattening onto the ground. Increased primary vertical half-height to 5m and Momentum Cleave to 6m in the aimed frame; damage numbers and forward reach are unchanged.
- Added perspective, side-view, pitched-aim, radius, lifetime, resource-cleanup and GPU finite-color checks.

## 0.1.3

- Fixed invalid vertex alpha at cleave arc endpoints, which could produce transient black strips.
- Added original-resource Storm Hammer impact and Great Cleave animation atlases, with a gameplay-sector mask and clear radius/side boundaries. Attack visuals now derive their dimensions from the same balance parameters as damage.
- Reworked the hammer flight core using original electrical sprite variants, broader short wakes and a bright gold-white center.
- God's Strength now commits to a 0.8-second cast, releasing its buff and burst at 0.55 seconds. Other skills and planar movement are locked during the cast.
- Momentum Cleave now uses a separate pair of source alternate attack animations for the classic and Immortal skins.
- Added finite-color, shared-range, deferred bright-ground, and committed-cast regression checks.

## 0.1.2

- Fixed attacks and cast animations failing to reach the detached in-game model. Three source attack animations now drive blade-following sword trails.
- Rebuilt Storm Hammer lightning, impact, and lingering trails; fixed projectile ghost initialization and lifetime.
- Added Warcry and God's Strength cast animations baked from original Dota particle resources, plus persistent armor, arm and weapon effects.
- Added shaded red/gold appearance variants, an expanded Immortal sword and a golden BKB rim while preserving native hit flashes and fading.
- Added actual Unity animator and presentation playback checks for both skins.

## 0.1.1

- Fixed startup stopping with a FieldAccessException while initializing Sven's network state machines. Added the Unity Mono access declarations required by the publicized game references.
- Added a regression check for those declarations and the installed game's state-machine field signature.

## 0.1.0

- First Sven playtest build with three relic choices and alternate secondary, utility and special skills.
- Added classic and three-Immortal/Stormwrought Arbiter appearances, source animations, skill sounds and effects.
- Added independent configuration, bilingual text and optional LookingGlass proc information.
- Momentum Cleave uses 2000% base damage plus 60% per m/s, including vertical movement.
