# Fateful Rush

**Fateful Rush** is a fast-paced 2D arcade survival game developed with **Unity 6** for Android.

The game is built around **40 handcrafted missions** that progressively introduce new enemies, hazards, abilities, mechanics, and increasingly difficult combinations of previously learned systems.

Rather than relying on a single endless survival loop, Fateful Rush uses multiple mission types, a structured progression system, dynamic difficulty configurations, combo-based risk/reward mechanics, boss encounters, unlockable cosmetics, and persistent player statistics.

The project focuses heavily on **responsive gameplay, mobile optimization, modular system design, enemy AI, audiovisual feedback, and polished game feel.**

---

# Gameplay

Players control a spaceman through a custom mobile control system and must complete different objectives while surviving an increasingly hostile arena.

Each mission can require the player to:

* Reach a target score.
* Survive for a specified amount of time.
* Reach a target score before time expires.
* Collect Normal, Gold, and Rare coins.
* Build and maintain combo chains.
* Dodge enemies, projectiles, lasers, bombs, and environmental obstacles.
* Use Dash and Void Clone abilities strategically.
* Collect temporary Armor and Slow Motion power-ups.
* Trigger Near Misses by narrowly avoiding danger.
* Survive Boss and Mini-Boss encounters.
* Complete missions to unlock new levels and cosmetic skins.

New mechanics are gradually introduced throughout the campaign before eventually being combined into more difficult mission configurations.

---

# Mission & Progression System

Fateful Rush currently contains **40 structured missions**.

Each mission is configured independently and can control:

* Win condition
* Score and time requirements
* Enemy combinations
* Enemy spawn rates
* Hazard combinations
* Danger levels
* Player movement speed
* Available abilities
* Power-up availability
* Coin types and probabilities
* Combo configuration
* Boss spawn conditions
* Arena obstacles
* Music
* Visual atmosphere

Before entering a mission, players can view a **Mission Briefing** containing the objective, difficulty rating, mission-specific information, and previous best time.

The progression system is designed to introduce mechanics gradually instead of exposing every system to the player immediately.

---

# Player Systems

## Movement

The player controller is designed specifically around responsive mobile movement.

Features include:

* Custom virtual joystick controls
* Smooth acceleration and deceleration
* Responsive direction changes
* Sharp-turn handling
* Analog input scaling
* Adjustable control layout
* Mobile-focused movement tuning

## Dash

Dash provides a short burst of movement that can be used both defensively and offensively.

It includes:

* Configurable distance, duration, and cooldown
* Collision-based interaction with certain enemies
* Dedicated visual and audio feedback
* Haptic feedback
* Skin-dependent trail visuals

## Void Clone

Void Clone creates a temporary decoy that redirects enemy targeting.

Different enemy types can react to the clone independently, allowing it to be used strategically to manipulate enemy positioning and create escape opportunities.

---

# Combo System

Collecting coins continuously builds a multi-stage combo chain.

The combo system currently scales from **2x to 6x** and can provide:

* Increased score rewards
* Movement speed bonuses
* Dedicated combo feedback
* Combo progression tracking
* Risk vs. reward gameplay

At higher combo stages, the player also activates a **Combo Magnet**.

At **5x and 6x combo**, nearby coins are smoothly pulled toward the player, with the 6x stage providing the strongest attraction.

Losing the chain resets these advantages.

---

# Near Miss System

Danger can also be rewarded.

Passing extremely close to enemies, projectiles, and certain hazards without being hit can trigger a **Near Miss**.

Near Misses provide:

* Temporary movement speed boost
* Near Miss streaks
* Distance-based feedback intensity
* Camera shake
* Positional sound feedback
* Haptic feedback
* Dedicated UI feedback
* Persistent statistics

Repeated Near Misses within a short period build a streak, encouraging aggressive and risky movement.

---

# Enemy System

Fateful Rush contains several enemy archetypes designed around different forms of pressure.

## Stalker

A pursuit-based enemy featuring:

* Predictive movement
* Progressive speed pressure
* Group separation
* Tactical pursuit roles
* Obstacle avoidance
* Anti-stuck navigation

Multiple Stalkers can approach the player differently instead of simply following the same path.

## Blaster

A ranged enemy that maintains combat distance while attacking the player.

Its behaviour includes:

* Range management
* Strafing
* Predictive aiming
* Enemy separation
* Obstacle avoidance
* Projectile pooling
* Movement variation

## Hunter

A high-speed charge enemy built around telegraphed attacks.

Hunters reposition themselves, display a warning before attacking, perform rapid charges, and enter recovery or stun states after certain interactions.

## Beacon

A mobile support enemy that increases the threat of other enemies while active.

Beacons can modify enemy behaviour and combat values such as:

* Movement speed
* Projectile behaviour
* Attack frequency
* Hunter timing and charge behaviour

The player can destroy Beacons using Dash, creating a priority-target decision during crowded encounters.

---

# Boss System

Boss encounters are designed as multi-stage gameplay events rather than standard pursuit enemies.

Boss mechanics include:

* Dedicated spawn sequences
* Stalker absorption
* Power-up phase
* Increasing visual feedback
* Screen-wide AOE attacks
* AOE warning phase
* Environmental cover detection
* Strike shockwaves
* Camera shake and dedicated sound design
* Boss splitting

During the Boss AOE attack, obstacles become important defensive tools: the player must position themselves behind valid cover to survive the strike.

Certain Boss configurations can split into **two Mini-Bosses**.

Mini-Bosses use coordinated pursuit behaviour and their own **localized AOE attacks**, creating a different combat phase after the original Boss encounter.

---

# Hazards & Arena

Enemies are only one source of danger.

The arena can also contain:

* Vertical Laser Walls
* Horizontal Laser Walls
* Space Bombs
* Static obstacles
* Moving obstacles

Hazards use configurable warning times, spawn behaviour, active durations, and danger levels.

Spawn systems also include player-safe placement checks to reduce unfair unavoidable situations.

---

# Danger Level System

Enemies and traps use a shared **five-tier Danger Level system**.

Different danger tiers can modify behaviour such as:

* Movement speed
* Prediction strength
* Attack frequency
* Projectile speed
* Hunter warning and charge timing
* Boss behaviour
* Beacon buffs
* Laser timing
* Bomb frequency

This allows difficulty to scale through behaviour and pressure instead of simply increasing the number of enemies.

Individual missions can also override shared balance values when a level requires unique tuning.

---

# Power-Ups

Temporary pickups provide tactical advantages during missions.

## Armor

Armor protects the player from a lethal hit and briefly provides immunity after breaking.

## Slow Motion

Temporarily slows gameplay threats, giving the player additional time to reposition and react.

Power-up availability and timing can be configured independently for each mission.

---

# Skin & Cosmetic Progression

Fateful Rush includes an unlockable cosmetic skin system tied directly to mission progression.

Skins can modify:

* Player appearance
* Dash trail
* Armor visuals
* Menu and UI accent colors

Certain skins also support additional cosmetic visual and audio effects.

Unlocked and selected skins are saved persistently between sessions.

---

# Statistics System

The game includes a persistent statistics system that tracks player progression and performance across runs.

Tracked statistics include:

### General

* Total runs
* Wins and deaths
* Win rate
* Win streaks
* Total play time
* Average and longest run

### Progression

* Completed missions
* Overall completion percentage
* Highest completed level
* Unlocked skins

### Performance

* Total score
* Best score
* Average score
* Highest combo
* Longest combo chain
* Combo bonus score

### Gameplay

* Coins collected by type
* Combo Magnet collections
* Near Misses and best Near Miss streak
* Dash and Clone usage
* Armor and Slow Motion usage

### Danger Mastery

* Beacons destroyed
* Hunters stunned
* Boss encounters
* Boss splits
* Boss AOE evasions
* Mini-Boss AOE evasions

The game also records **death causes**, identifies the player's most common nemesis, and stores individual **best times for completed missions**.

---

# Audio & Game Feel

Audio and feedback systems are designed to communicate gameplay state clearly without overwhelming the player.

Features include:

* Per-mission gameplay music
* Dynamic music tension based on mission progress
* Smooth music transitions
* Dedicated Audio Mixer routing
* Gameplay, UI, and critical SFX channels
* Positional 3D gameplay audio
* SFX pitch and volume variation
* Boss-specific audio states
* Slow Motion audio response
* Near Miss audio
* Haptic feedback
* Camera shake
* Spawn effects
* Death and victory feedback
* Animated HUD and menu transitions

Repeated gameplay sounds use subtle variation to reduce audio repetition during longer sessions.

---

# Mobile Optimization

Fateful Rush is designed primarily for mobile hardware.

Performance systems include:

* Runtime object pooling
* Projectile pooling
* Particle reuse
* Optimized spawn systems
* Mobile UI optimizations
* Reduced unnecessary UI raycasts
* Configurable frame-rate targeting
* Unity Adaptive Performance integration
* Thermal throttling detection
* Adaptive render-scale reduction under thermal pressure
* Automatic performance recovery

The goal is to maintain consistent gameplay responsiveness while reducing unnecessary allocations and runtime overhead.

---

# Technical Architecture

The project uses a modular and data-driven structure to keep gameplay systems configurable and reusable.

Technical highlights include:

* Unity ScriptableObject-based level configuration
* Shared Danger Balance profiles
* Data-driven mission design
* Modular enemy behaviours
* Reusable spawning systems
* Runtime object pooling
* Physics-based 2D obstacle steering
* Predictive avoidance of moving obstacles
* Enemy separation and tactical pursuit
* Event-based gameplay communication
* Persistent progression and settings
* Modular audio routing
* Mobile control-layout management
* Reusable UI animation systems
* Custom editor tooling for level and balance configuration

The majority of gameplay behaviour can be tuned through configuration data and Inspector values without rewriting individual systems for every mission.

---

# Technology Stack

* **Unity 6**
* **C#**
* **Universal Render Pipeline (URP)**
* **Unity Input System**
* **Unity Adaptive Performance**
* **Visual Studio**
* **Git**
* **GitHub**

---

# Platform

* **Android**

---

# Development Status

Fateful Rush is currently in active development and release preparation.

Current development is focused primarily on:

* Gameplay balancing
* Enemy AI improvements
* Bug fixing
* Mobile performance
* Audio and visual polish
* Mission tuning
* UI/UX refinement
* Final release preparation

---

# Project Goal

Fateful Rush is being developed both as a complete mobile game and as a portfolio project demonstrating practical Unity game development.

The project focuses on building and maintaining interconnected gameplay systems including:

* Player movement and abilities
* Enemy AI
* Mission progression
* Dynamic difficulty
* Boss encounters
* Data-driven balancing
* Mobile performance
* Audio systems
* Persistent statistics
* UI/UX
* Gameplay feedback
* Reusable and maintainable C# systems

The objective is not only to complete the game, but to build it using systems that can be expanded, balanced, debugged, and maintained throughout production.
