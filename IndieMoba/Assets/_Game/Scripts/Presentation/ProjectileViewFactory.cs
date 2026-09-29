using UnityEngine;

namespace IndieMoba.Presentation
{
    public interface IProjectileViewFactory
    {
        GameObject Acquire(GameObject prefab, Vector3 position, Transform parent);
        void Release(GameObject instance);
    }

    public sealed class InstantiateProjectileViewFactory : IProjectileViewFactory
    {
        public GameObject Acquire(GameObject prefab, Vector3 position, Transform parent)
        {
            return Object.Instantiate(prefab, position, Quaternion.identity, parent);
        }

        public void Release(GameObject instance)
        {
            Object.Destroy(instance);
        }
    }
}
