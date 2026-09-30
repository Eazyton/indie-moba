using UnityEngine;

namespace IndieMoba.Minions
{
    public interface IMinionFactory
    {
        MinionController Spawn(MinionController prefab, Vector2 position, in MinionSpawnContext context, Transform parent);
    }

    public sealed class InstantiateMinionFactory : IMinionFactory
    {
        public MinionController Spawn(MinionController prefab, Vector2 position, in MinionSpawnContext context, Transform parent)
        {
            MinionController instance = Object.Instantiate(prefab, new Vector3(position.x, position.y, 0f), Quaternion.identity, parent);
            instance.name = $"{prefab.name}_{context.SpawnIndex}";
            instance.Initialize(context);
            return instance;
        }
    }
}
