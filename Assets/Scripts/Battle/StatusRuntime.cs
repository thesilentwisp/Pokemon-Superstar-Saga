using System.Collections.Generic;

/// <summary>
/// Runtime status list helpers and tick handling.
/// </summary>

public class StatusInstance
{
    public StatusType type;
    public int turnsLeft;
    public StatusInstance(StatusType t, int d) { type = t; turnsLeft = d;}
}

public static class StatusRuntime
{
    public static void TickEndOfTurn(List<StatusInstance> list)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            list[i].turnsLeft--;
            if (list[i].turnsLeft <= 0) list.RemoveAt(i);
        }
    }

    public static bool Has(this List<StatusInstance> list, StatusType t)
    {
        foreach (var s in list) if (s.type == t) return true;
        return false;
    }
}
