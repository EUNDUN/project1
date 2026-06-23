using UnityEngine;

namespace Game.UI
{
    // Draws a four-line crosshair at screen center using immediate-mode GUI.
    // No per-frame allocation: Rect is a value type, Texture2D.whiteTexture is engine-owned.
    public class CrosshairUI : MonoBehaviour
    {
        public Color color = Color.white;
        public int lineLength = 8;
        public int lineThickness = 2;
        public int gap = 4;

        void OnGUI()
        {
            int cx = Screen.width / 2;
            int cy = Screen.height / 2;
            int ht = lineThickness / 2;

            GUI.color = color;
            GUI.DrawTexture(new Rect(cx - gap - lineLength, cy - ht, lineLength, lineThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + gap,              cy - ht, lineLength, lineThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - ht, cy - gap - lineLength, lineThickness, lineLength), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - ht, cy + gap,              lineThickness, lineLength), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
