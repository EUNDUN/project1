using UnityEngine;
using Game.Player;

namespace Game.UI
{
    // Debug panel (top-left corner) — shows FirstPersonMotor movement calculation breakdown.
    // Strings are rebuilt every RefreshInterval seconds, not per frame.
    // Wire motor from GameBootstrap after motor is created.
    // Remove or disable before shipping.
    public class MotorDebugUI : MonoBehaviour
    {
        public FirstPersonMotor motor;

        private const float RefreshInterval = 0.15f;
        private float _refreshTimer;

        // Cached display strings — rebuilt at RefreshInterval, never in OnGUI.
        private string _lineInput  = "MoveInput  (0.00,0.00)";
        private string _lineBase   = "BaseSpeed  0.00";
        private string _lineMotor  = "MotorMult  0.00";
        private string _lineDebuff = "DebuffMult 0.00";
        private string _lineSelf   = "SelfMult   0.00";
        private string _lineFinal  = "FinalSpeed 0.00";
        private string _lineDash   = "DashVel    (0.0,0.0,0.0)";
        private string _lineMotion = "Motion     (0.0,0.0,0.0)";

        private static GUIStyle s_style;

        private const float PanelX = 8f;
        private const float PanelY = 8f;
        private const float PanelW = 234f;
        private const float LineH  = 18f;
        private const float PadX   = 6f;
        private const float PadY   = 4f;
        private const int   Lines  = 8;

        void Start()
        {
            if (motor == null) { enabled = false; return; }
        }

        void Update()
        {
            _refreshTimer -= Time.deltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = RefreshInterval;

            _lineInput  = "MoveInput  " + FV2(motor.DebugMoveInput);
            _lineBase   = "BaseSpeed  " + F2(motor.DebugBaseSpeed);
            _lineMotor  = "MotorMult  " + F2(motor.DebugMotorMoveMultiplier);
            _lineDebuff = "DebuffMult " + F2(motor.DebugDebuffMultiplier);
            _lineSelf   = "SelfMult   " + F2(motor.DebugSelfMoveMultiplier);
            _lineFinal  = "FinalSpeed " + F2(motor.DebugFinalSpeed);
            _lineDash   = "DashVel    " + FV3(motor.DebugDashVelocity);
            _lineMotion = "Motion     " + FV3(motor.DebugMotion);
        }

        void OnGUI()
        {
            if (s_style == null)
                s_style = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = 12,
                    alignment = TextAnchor.MiddleLeft,
                    normal    = { textColor = Color.white },
                };

            float panelH = PadY * 2f + Lines * LineH;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(PanelX, PanelY, PanelW, panelH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float x = PanelX + PadX;
            float y = PanelY + PadY;
            GUI.Label(new Rect(x, y, PanelW - PadX, LineH), _lineInput,  s_style); y += LineH;
            GUI.Label(new Rect(x, y, PanelW - PadX, LineH), _lineBase,   s_style); y += LineH;
            GUI.Label(new Rect(x, y, PanelW - PadX, LineH), _lineMotor,  s_style); y += LineH;
            GUI.Label(new Rect(x, y, PanelW - PadX, LineH), _lineDebuff, s_style); y += LineH;
            GUI.Label(new Rect(x, y, PanelW - PadX, LineH), _lineSelf,   s_style); y += LineH;
            GUI.Label(new Rect(x, y, PanelW - PadX, LineH), _lineFinal,  s_style); y += LineH;
            GUI.Label(new Rect(x, y, PanelW - PadX, LineH), _lineDash,   s_style); y += LineH;
            GUI.Label(new Rect(x, y, PanelW - PadX, LineH), _lineMotion, s_style);
        }

        // Called once every RefreshInterval — string allocation here is intentional and bounded.
        static string F2(float v)    => v.ToString("F2");
        static string FV2(Vector2 v) => "(" + v.x.ToString("F2") + "," + v.y.ToString("F2") + ")";
        static string FV3(Vector3 v) => "(" + v.x.ToString("F1") + "," + v.y.ToString("F1") + "," + v.z.ToString("F1") + ")";
    }
}
