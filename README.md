# Status Syetem
System to store status and manages them collectively.

# Usage

1. Define the key to use for Status.
```csharp
public partial class Status
{
    //define
    public enum Key
    {
        //e.g.
        Hp,
        Mp,
        Speed,
        CriticalProbability
        //...

        End //'End' must be last.
    }
}
```

2. Defines the status and modifier in Unit.
```csharp
public class Player : MonoBehaviour
{
    public Status maxStatus = new();
    public Status curStatus = new();
    public Status.Modifier modifier = new();
}
```

3. Connect references to apply changes.

```csharp
public class Player : MonoBehaviour
{
    public Status maxStatus = new();
    public Status curStatus = new();
    public Status.Modifier modifier = new();

    private Status BaseStatus = new();

    private void Awake()
    {
        modifier.onValueChange += key => modifier.Calculate(key, maxStats, BaseStatus);
        maxStatus.onStatChanged += (key, old, cur) =>
        {
            var diff = cur - old;
            curStatus.SetStatus(key, x => x + diff);
        };
    }
}
```

4. Modify the Status as needed.
```csharp
//e.g.
public class HealItem
{
    //heal player hp 10
    public void Use(Player player)
    {
        player.curStatus.SetStatus(Status.Key.Hp, hp => hp + 10);
    }
}

public class VeryAwesomeArmor
{
    //gain player 10% max hp
    public void Wear(Player player)
    {
        player.modifier.Set(
            "VeryAwesomeArmor", 
            Status.Key.Hp, 
            percent => 0.1,
            add => 0
        );
    }
}

public class Player : MonoBehaviour
{
    //...

    public void Interact()
    {
        HealItem item = new();
        item.Use(this);
        
        VeryAwesomeArmor armor = new();
        armor.Wear(this);
    }  
}
```

# Dependency
[UniRx](https://github.com/neuecc/UniRx/releases/tag/7.1.0)

# Lisence
MIT Lisence