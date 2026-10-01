using UnityEngine;

namespace IndieMoba.Map
{
    [CreateAssetMenu(menuName = "IndieMoba/Map/Map Layout", fileName = "MapLayout")]
    public sealed class MapLayoutData : ScriptableObject
    {
        public const int CurrentVersion = 1;

        [Min(0.01f)] [SerializeField] private float scale = 1f;
        [Min(1f)] [SerializeField] private float playableSize = 128f;
        [Min(0f)] [SerializeField] private float apronMargin = 8f;
        [Min(0f)] [SerializeField] private float borderThickness = 3f;
        [SerializeField] private Rect cameraBounds = new Rect(-6f, -6f, 140f, 140f);
        [Min(0.1f)] [SerializeField] private float orthographicSize = 9.375f;
        [SerializeField] private MapCollisionMode collisionMode = MapCollisionMode.FilledPolygons;
        [Range(6, 64)] [SerializeField] private int circleSegments = 16;
        [SerializeField] private Vector2 blueFountain = new Vector2(7f, 7f);
        [SerializeField] private Vector2 redFountain = new Vector2(121f, 121f);
        [SerializeField] private MapLaneLayout[] lanes;
        [SerializeField] private MapStructureSlot[] structures;
        [SerializeField] private MapShape[] walkable;
        [SerializeField] private MapShape[] blockers;
        [SerializeField] private MapZoneLayout[] zones;

        public float Scale { get => scale; set => scale = Mathf.Max(0.01f, value); }
        public float PlayableSize { get => playableSize; set => playableSize = value; }
        public float ApronMargin { get => apronMargin; set => apronMargin = value; }
        public float BorderThickness { get => borderThickness; set => borderThickness = value; }
        public Rect CameraBounds { get => cameraBounds; set => cameraBounds = value; }
        public float OrthographicSize { get => orthographicSize; set => orthographicSize = value; }
        public MapCollisionMode CollisionMode { get => collisionMode; set => collisionMode = value; }
        public int CircleSegments { get => circleSegments; set => circleSegments = Mathf.Clamp(value, 6, 64); }
        public Vector2 BlueFountain { get => blueFountain; set => blueFountain = value; }
        public Vector2 RedFountain { get => redFountain; set => redFountain = value; }
        public MapLaneLayout[] Lanes { get => lanes; set => lanes = value; }
        public MapStructureSlot[] Structures { get => structures; set => structures = value; }
        public MapShape[] Walkable { get => walkable; set => walkable = value; }
        public MapShape[] Blockers { get => blockers; set => blockers = value; }
        public MapZoneLayout[] Zones { get => zones; set => zones = value; }

        public Vector2 ToWorld(Vector2 layoutPoint)
        {
            return layoutPoint * scale;
        }

        public Rect ScaledCameraBounds => new Rect(cameraBounds.position * scale, cameraBounds.size * scale);

        public bool HasContent => lanes != null && lanes.Length > 0 && walkable != null && walkable.Length > 0;
    }
}
