using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class StaticHelpers
{
    public static async void JustRun(this Task toStart)
    {
        await toStart;
    }
}
