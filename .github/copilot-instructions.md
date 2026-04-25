# Copilot Instructions for Color Block Jam

## Project Overview
This project appears to be a Unity-based game development project. The folder structure and file naming conventions suggest the use of Unity's standard practices, with additional customizations for modularity and organization.

### Key Components
- **Assets/**: Contains all Unity assets, including scripts, prefabs, scenes, and resources.
  - **Scripts/**: Core game logic and systems.
  - **Prefabs/**: Reusable game objects.
  - **Scenes/**: Unity scenes for different game levels or states.
  - **Resources/**: Assets loaded dynamically at runtime.
- **ProjectSettings/**: Unity project configuration files.
- **Packages/**: Manages Unity packages and dependencies.

### External Dependencies
- **DOTween**: A Unity tweening library for animations.
- **Sirenix.OdinInspector**: Enhances Unity's inspector and serialization capabilities.

## Developer Workflows

### Building the Project
1. Open the project in Unity.
2. Use the Unity Editor's "Build Settings" to configure the target platform.
3. Click "Build and Run" to generate the executable.

### Testing
- Use Unity's Play Mode to test the game.
- Write and run tests using Unity Test Framework.

### Debugging
- Use Unity's Console for runtime logs.
- Attach a debugger to the Unity Editor for step-through debugging.

## Project-Specific Conventions

### Code Organization
- Scripts are modular and grouped by functionality (e.g., `Game.GridSystem`, `Game.Management`).
- Use namespaces to avoid naming conflicts.

### Patterns
- **Singletons**: Commonly used for game managers.
- **Scriptable Objects**: For data-driven design and configuration.
- **Event-Driven Architecture**: For decoupled communication between systems.

### Naming Conventions
- Classes and files use PascalCase.
- Private fields use camelCase with an underscore prefix (e.g., `_exampleField`).
- Public fields and properties use PascalCase.

## Integration Points
- **DOTween**: Used for animations. Check `DOTween.Modules.csproj` for setup.
- **Odin Inspector**: Enhances editor workflows. Refer to `Sirenix.OdinInspector.Modules.*` files.

## Examples
- **Grid System**: Refer to `Game.GridSystem.csproj` for grid-based logic.
- **Management**: Check `Game.Management.csproj` for game state management.

## Tips for AI Agents
- Follow Unity's standard practices unless specified otherwise.
- Look for `Scriptable Objects` in `Assets/Scriptable Objects/` for configurable data.
- Use the `Scenes/` folder to identify entry points for gameplay.

---

Feel free to update this file as the project evolves!