using UnityEngine;
using Game.Commands;

namespace Game.Input
{
    // Owns no state — purely a translator.
    public class PlayerInputReader : MonoBehaviour
    {
        public PlayerCommand Read()
        {
            return new PlayerCommand
            {
                MoveInput = new Vector2(
                    UnityEngine.Input.GetAxisRaw("Horizontal"),
                    UnityEngine.Input.GetAxisRaw("Vertical")),
                LookInput = new Vector2(
                    UnityEngine.Input.GetAxis("Mouse X"),
                    UnityEngine.Input.GetAxis("Mouse Y")),
                JumpPressed   = UnityEngine.Input.GetButtonDown("Jump"),
                CrouchHeld    = UnityEngine.Input.GetKey(KeyCode.LeftControl)
                             || UnityEngine.Input.GetKey(KeyCode.C),
                AttackPressed = UnityEngine.Input.GetButtonDown("Fire1")
            };
        }
    }
}