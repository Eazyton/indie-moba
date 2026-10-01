using UnityEngine;

namespace IndieMoba.Map
{
    [DisallowMultipleComponent]
    public sealed class MapGenerationStamp : MonoBehaviour
    {
        [SerializeField] private string layoutGuid;
        [SerializeField] private string layoutPath;
        [SerializeField] private int generatorVersion;
        [SerializeField] private string generatedAtUtc;

        public string LayoutGuid => layoutGuid;
        public string LayoutPath => layoutPath;
        public int GeneratorVersion => generatorVersion;
        public string GeneratedAtUtc => generatedAtUtc;

        public void Configure(string guid, string path, int version, string timestampUtc)
        {
            layoutGuid = guid;
            layoutPath = path;
            generatorVersion = version;
            generatedAtUtc = timestampUtc;
        }
    }
}
