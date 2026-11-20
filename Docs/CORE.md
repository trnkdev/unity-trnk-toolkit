# TRnK.Toolkit Core

Core foundational systems: singletons, pooling, and color swatches.

## Singletons

```csharp
using TRnK.Singleton;
```

### PersistentSingleton

Survives scene changes. Perfect for game managers.

```csharp
public class GameManager : PersistentSingleton<GameManager>
{
    public int Score { get; set; }
}

// Usage
GameManager.Instance.Score += 100;
```

### Color Swatch

```csharp
using TRnK.ColorPalette;
```

Pre-defined color constants for consistent theming.

```csharp
// Available colors
Color darkGray = Swatch.DG;
Color vibrantRed = Swatch.VR;

// Usage in debug messages
Debug.Log("Success!".Colorize(Swatch.DE));
Debug.LogError("Error!".Colorize(Swatch.VR));

// UI theming
button.color = Swatch.VC;
errorText.color = Swatch.VR;
```

### Log

```csharp
using TRnK.Logger;
```

Simple conditional logger. Methods are compiled only in the Editor, Development builds, or when `TRNK_LOG` is defined.

```csharp
using TRnK.Logger;

// Basic usage
Log.Info("Started");
Log.Warn("Potential issue");
Log.Error("Something went wrong");

// With context to ping an object in the Console
Log.Info("Found object", someGameObject);
```
