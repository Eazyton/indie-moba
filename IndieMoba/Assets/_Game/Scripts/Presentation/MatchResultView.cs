using UnityEngine;
using UnityEngine.SceneManagement;
using IndieMoba.Match;

namespace IndieMoba.Presentation
{
    public sealed class MatchResultView : MonoBehaviour
    {
        [SerializeField] private MatchController match;
        [SerializeField] private float referenceHeight = 720f;
        [SerializeField] private bool showRestartButton = true;

        private GUIStyle titleStyle;

        private void Awake()
        {
            if (match == null)
            {
                match = FindAnyObjectByType<MatchController>();
            }
        }

        private void OnGUI()
        {
            if (match == null || match.State == MatchState.Playing)
            {
                return;
            }
            float scale = Mathf.Max(1f, Screen.height / referenceHeight);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 64,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }
            bool victory = match.IsLocalVictory;
            titleStyle.normal.textColor = victory ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(1f, 0.35f, 0.3f, 1f);
            GUI.Box(new Rect(0f, height * 0.5f - 90f, width, 180f), GUIContent.none);
            GUI.Label(new Rect(0f, height * 0.5f - 80f, width, 100f), victory ? "VICTORY" : "DEFEAT", titleStyle);
            if (showRestartButton && GUI.Button(new Rect(width * 0.5f - 70f, height * 0.5f + 30f, 140f, 36f), "Restart"))
            {
                Scene scene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
            }
        }
    }
}
