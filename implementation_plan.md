# Context-Aware Reminder System for Inky

This document outlines the approach to implementing the context-aware reminder system for Inky using modular, plug-in style C# scripts that do not break or alter existing functionalities like `CompanionVisualController`.

## User Review Required

> [!IMPORTANT]  
> Please review the AI Integration Approach. 
> I plan to provide a simple `UnityWebRequest` setup targeting the OpenAI Chat Completions API as the primary AI logic. You would just need to paste an API key in the Inspector. A robust Rule-Based fallback (dictionaries) will be implemented if the API call fails or no key is provided. Is this acceptable, or would you prefer a strictly local on-device small model approach (which requires compiling C++ libraries or heavy packages)?

> [!IMPORTANT]  
> Please review the "Look At" behavior.
> The requirements ask for Inky to "Look at the prop" and "look at the user". Since I cannot modify the rig or IK setup, I propose two non-intrusive ways: 
> 1) Softly rotating Inky's entire root Transform towards the prop/user using `Quaternion.Slerp`. 
> 2) If Inky already has a LookAt/IK target script, I can hook into that instead. 
> I will proceed with Option 1 (Root rotation) unless you advise otherwise.

## Proposed Changes

We will introduce a new folder `Assets/Scripts/ContextSystem` containing all modular scripts to keep things organized.

---

### Core Data and Settings

#### [NEW] `Assets/Scripts/ContextSystem/ReminderData.cs`
A simple struct/class to hold reminder text, time remaining, and extracted category.

---

### AI & Context Processing

#### [NEW] `Assets/Scripts/ContextSystem/ContextExtractor.cs`
Handles the extraction of intention/category from a string.
- **Primary Method:** An async `UnityWebRequest` to OpenAI's API (e.g. `gpt-3.5-turbo` or `gpt-4o-mini`) asking it to classify the user's intent into predefined categories (Sports, Study, Fitness, Social, General).
- **Secondary Method:** Rule-based fallback using `Contains()` or regex on keywords. Example: keywords `["gym", "workout", "weights"]` map to `Fitness`.

---

### Prop Management

#### [NEW] `Assets/Scripts/ContextSystem/PropManager.cs`
Handles the dictionary mapping and safe instantiation of props.
- Uses a `SerializableDictionary` or parallel Lists to map Category Enums/Strings to Prefabs.
- Handles spawning logic: Uses a calculated offset from Inky's position to ensure it is visible but does not clip into Inky's mesh.
- Exposes a fallback default prefab (the "Notification Orb").

---

### Main Controller & Behavior

#### [NEW] `Assets/Scripts/ContextSystem/InkyContextController.cs`
The main "plug-in" component to attach alongside `CompanionVisualController`.
- Accepts new reminders and passes them to `ContextExtractor`.
- On context received, tells `PropManager` to spawn the relevant prop.
- Monitors time remaining. Maps time ranges to the `CompanionVisualController.urgencyScore` (e.g., >30m = 0.0, 10-30m = 0.3, <10m = 0.7, <5m = 1.0) so existing VFX and colors work seamlessly.
- Handles rotating Inky's root transform to face the prop or camera based on the urgency level.

---

### Debugging & Testing

#### [NEW] `Assets/Scripts/ContextSystem/ReminderTestDashboard.cs`
A MonoBehaviour script that can be attached to an empty GameObject to provide a simple GUI (via `OnGUI` or a custom Editor window) for rapid testing.
- Input field for Reminder String.
- Input field for Time Remaining (mins).
- "Simulate Reminder" button.
- Debug text showing extracted category, current urgency, and active prop.
- Uses `OnDrawGizmos` in `PropManager` to visualize where props will spawn relative to Inky.

## Open Questions

1. Do you have specific prefabs ready for "Football", "Study", "Gym" and the "Default Orb", or should I set up the scripts to accept any prefab you drag into the Inspector?
2. Concerning animations: `CompanionVisualController` already drives the `AnxietyLevel` float in the Animator. Do you have discrete trigger parameters (e.g., "Interact") you want me to fire when interacting with the prop, or should I rely solely on the existing `AnxietyLevel` logic?

## Verification Plan

### Manual Verification
1. I will write the scripts and attach them to a test prefab or ask you to attach `InkyContextController` to Inky.
2. We will use the `ReminderTestDashboard` inside the Unity Editor (Play Mode).
3. Test Case 1: Enter "football match at 6 pm" with 45 mins. Expect "Sports" category, Idle behaviour, and a sports prop.
4. Test Case 2: Enter "study for exam" with 8 mins. Expect "Study" category, Active behaviour, a book prop, and `AnxietyLevel` increasing.
5. Verification of the Fallback: Disconnect internet or provide no API key, enter "go to gym". Expect the rule-based fallback to assign "Fitness" and spawn the Dumbbell.
