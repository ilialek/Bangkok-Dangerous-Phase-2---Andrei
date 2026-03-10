# Customizable Healing Items System

## Overview
This system allows you to create multiple healing item types with different properties:
- **Amount of Healing**: Total HP restored
- **Time Active**: How long the effect lasts
- **Speed Multiplier**: Player movement speed modifier
- **Damage Multiplier**: Player attack damage modifier
- **Time Slow**: Slows down enemies (affects global time scale)
- **Custom Visuals**: Each item can have its own sprite and particle effect

## Quick Setup

### 1. Create a Healing Item Data Asset
1. Right-click in Project window → `Create > Items > Healing Item Data`
2. Name it (e.g., "HealthPotion", "SpeedBoost", "DamageFood")
3. Configure the properties in the Inspector:
   - **Item Name**: Display name
   - **Item Sprite**: UI icon (shown in player's inventory slots)
   - **Heal Amount**: Total HP to restore (e.g., 25)
   - **Heal Duration**: How long healing lasts (e.g., 5 seconds)
   - **Heal Tick Rate**: How often healing applies (e.g., 0.5 = every half second)
   - **Speed Multiplier**: 1 = normal, 1.5 = 50% faster, 0.5 = 50% slower
   - **Damage Multiplier**: 1 = normal, 2 = double damage, 0.5 = half damage
   - **Enemy Time Scale**: 0.5 = enemies move at 50% speed (0 = frozen, 1 = normal)
   - **Applies Time Slow**: Check to enable time dilation
   - **Active Particle Effect**: Particle system prefab to spawn on player while active

### 2. Create Healing Item Pickup (Scene Object)
1. Create a GameObject in your scene (e.g., Cube, Sphere, or custom model)
2. Add `HealingItem` component
3. Set the tag to `HealingItem`
4. Assign your created data asset to the `Item Data` field
5. Optionally assign `Item Renderer` and `Item Collider` for visual feedback
6. Add a Collider component set to `Is Trigger`

### 3. Update Player Setup
Your `PlayerHealthSystem` is already updated to work with the new system. Just make sure:
- Pickup Slot 1 and Pickup Slot 2 UI Images are assigned
- Empty Sprite is assigned (optional, for empty slot visual)

## Example Item Configurations

### Basic Health Potion
- Heal Amount: 25
- Heal Duration: 5s
- Speed Multiplier: 1 (no change)
- Damage Multiplier: 1 (no change)
- Time Slow: Off

### Speed Boost Item
- Heal Amount: 10
- Heal Duration: 8s
- Speed Multiplier: 1.5 (50% faster)
- Damage Multiplier: 1
- Time Slow: Off

### Damage Buff Food
- Heal Amount: 15
- Heal Duration: 10s
- Speed Multiplier: 1
- Damage Multiplier: 2 (double damage)
- Time Slow: Off

### Time Warp Item
- Heal Amount: 5
- Heal Duration: 6s
- Speed Multiplier: 1.2
- Damage Multiplier: 1.5
- Enemy Time Scale: 0.5 (enemies 50% slower)
- Applies Time Slow: On

## Accessing Item Modifiers in Other Scripts

Other scripts can read the active modifiers from `PlayerHealthSystem`:

```csharp
PlayerHealthSystem healthSystem = GetComponent<PlayerHealthSystem>();

// Get current speed multiplier
float speedMod = healthSystem.SpeedMultiplier;

// Get current damage multiplier
float damageMod = healthSystem.DamageMultiplier;

// Apply to your character controller
characterController.speed = baseSpeed * speedMod;

// Apply to your combat system
finalDamage = baseDamage * damageMod;
```

## Notes
- **Multiple Items**: If player consumes multiple items, only the most recent item's modifiers are active (simplified version)
- **Time Slow**: Currently affects global `Time.timeScale`, which impacts everything except unscaled operations
- **Stacking**: Healing stacks (can have multiple healing effects active), but modifiers use the last consumed item
- **Player Movement**: You'll need to integrate `SpeedMultiplier` into your movement controller
- **Combat Damage**: You'll need to integrate `DamageMultiplier` into your combat system

## Future Enhancements
To support full modifier stacking (e.g., 2 items both applying speed boosts):
1. Store a list of active item effects with remaining duration
2. Sum or multiply all active modifiers each frame
3. Remove modifiers as their durations expire

For player-only time exclusion (slow enemies but not player):
1. Create custom time system for enemies
2. Each enemy updates with `Time.deltaTime * EnemyTimeScale` instead of raw `Time.deltaTime`
