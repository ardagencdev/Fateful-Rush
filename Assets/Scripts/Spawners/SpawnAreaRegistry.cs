using System.Collections.Generic;
using UnityEngine;

public static class SpawnAreaRegistry
{
    private class Entry
    {
        public Transform transform;
        public float radius;
    }

    private static readonly List<Entry> entries = new List<Entry>(64);

    public static bool IsAreaFree(Vector2 pos, float radius)
    {
        Cleanup();

        float r;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].transform == null) continue;

            r = radius + entries[i].radius;
            if (((Vector2)entries[i].transform.position - pos).sqrMagnitude < r * r)
                return false;
        }

        return true;
    }

    public static void Register(GameObject obj, float radius)
    {
        if (obj == null) return;

        entries.Add(new Entry
        {
            transform = obj.transform,
            radius = radius
        });
    }

    private static void Cleanup()
    {
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i].transform == null)
                entries.RemoveAt(i);
        }
    }
}