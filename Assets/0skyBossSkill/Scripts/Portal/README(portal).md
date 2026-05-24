# Portal Scene Transition System

This portal system allows the player to:

- Move from one scene to another
- Spawn at a specific position in the destination scene
- Reuse the same scripts for any portal connection in the game

Example:

```text
finalBoss0 Portal
        ↓
Loads finalBoss1
        ↓
Player appears at the PortalOut position in finalBoss1
```

---

## Scripts Included

### 1. `PortalToScene.cs`

This script should be attached to the portal trigger object.

When the player enters the trigger area, it:

1. Stores the destination spawn point name
2. Loads the target scene

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalToScene : MonoBehaviour
{
    [Header("Target Scene Name")]
    [SerializeField] private string targetSceneName;

    [Header("Spawn Point Name In Target Scene")]
    [SerializeField] private string targetSpawnPointName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PortalSpawnData.nextSpawnPointName = targetSpawnPointName;
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
```

---

### 2. `PortalSpawnData.cs`

This script temporarily stores the name of the spawn point that the player should use after entering the next scene.

```csharp
public static class PortalSpawnData
{
    public static string nextSpawnPointName;
}
```

This script does **not** need to be attached to any GameObject.  
It only needs to exist in the project.

---

### 3. `PlayerSpawnAtPortal.cs`

This script should be attached to the Player.

When the new scene starts, it:

1. Reads the stored spawn point name
2. Finds that GameObject in the scene
3. Moves the player to that position

```csharp
using UnityEngine;

public class PlayerSpawnAtPortal : MonoBehaviour
{
    private void Start()
    {
        if (string.IsNullOrEmpty(PortalSpawnData.nextSpawnPointName))
        {
            return;
        }

        GameObject spawnPoint = GameObject.Find(PortalSpawnData.nextSpawnPointName);

        if (spawnPoint != null)
        {
            transform.position = spawnPoint.transform.position;
        }
        else
        {
            Debug.LogWarning("Could not find portal spawn point: " + PortalSpawnData.nextSpawnPointName);
        }

        PortalSpawnData.nextSpawnPointName = "";
    }
}
```

---

# How to Set Up a Portal

## Step 1: Add Scenes to Build Profiles

Before scene switching can work, both scenes must be added to:

```text
File → Build Profiles → Scene List
```

Example:

```text
finalBoss0
finalBoss1
```

---

## Step 2: Create the Portal Trigger

In the source scene:

1. Create an empty GameObject
2. Name it something like:

```text
PortalTrigger
```

3. Add:

```text
Box Collider 2D
```

4. Enable:

```text
Is Trigger
```

5. Attach:

```text
PortalToScene.cs
```

---

## Step 3: Configure the Portal

In the Inspector for `PortalTrigger`, fill in:

```text
Target Scene Name
```

and

```text
Spawn Point Name In Target Scene
```

Example for:

```text
finalBoss0 → finalBoss1
```

Use:

```text
Target Scene Name:
finalBoss1
```

```text
Spawn Point Name In Target Scene:
PortalOut_FinalBoss0
```

---

## Step 4: Create the Spawn Point in the Destination Scene

Open the destination scene.

1. Create an empty GameObject
2. Name it exactly the same as the spawn point name used in the portal settings

Example:

```text
PortalOut_FinalBoss0
```

3. Move it to the exact position where the player should appear after entering the portal

---

## Step 5: Attach the Player Spawn Script

Attach:

```text
PlayerSpawnAtPortal.cs
```

to the Player object.

This only needs to be done once.  
After that, all portals can use the same spawning system.

---

# Example Setup

## In `finalBoss0`

Create:

```text
PortalTrigger
```

Add:

```text
Box Collider 2D
Is Trigger = true
PortalToScene.cs
```

Set:

```text
Target Scene Name = finalBoss1
Spawn Point Name In Target Scene = PortalOut_FinalBoss0
```

---

## In `finalBoss1`

Create an empty GameObject named:

```text
PortalOut_FinalBoss0
```

Place it where the player should appear.

---

# Reusing This System

This setup is not limited to `finalBoss0` and `finalBoss1`.

It can be reused for any scene transition, such as:

```text
Forest → Cave
Town → Castle
Level1 → Level2
BossRoomEntrance → BossRoom
```

Only the Inspector values need to change:

```text
Target Scene Name
Spawn Point Name In Target Scene
```

---

# Important Notes

- The Player must have the tag:

```text
Player
```

- The portal trigger must have:

```text
Box Collider 2D
Is Trigger = true
```

- The destination spawn point name must match exactly
- The target scene must be included in the Build Profiles Scene List
