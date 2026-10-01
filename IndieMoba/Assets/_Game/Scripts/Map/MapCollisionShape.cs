using UnityEngine;

namespace IndieMoba.Map
{
    public sealed class MapCollisionShape : MonoBehaviour
    {
        [SerializeField] private string shapeId;
        [SerializeField] private MapShapeRole role;
        [SerializeField] private MapRegion region;

        public string ShapeId => shapeId;
        public MapShapeRole Role => role;
        public MapRegion Region => region;

        public void Configure(string id, MapShapeRole shapeRole, MapRegion shapeRegion)
        {
            shapeId = id;
            role = shapeRole;
            region = shapeRegion;
        }
    }
}
