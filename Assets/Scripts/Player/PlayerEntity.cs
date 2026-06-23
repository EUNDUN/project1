using UnityEngine;

namespace Game.Player
{
    // Remote players will have this component but no LocalPlayerController.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerEntity : MonoBehaviour
    {
        public string PlayerId { get; private set; }
        public CharacterController Controller { get; private set; }

        // Simulation state — flat data, easy to serialize for netcode
        public Vector3 Velocity { get; set; }
        public bool IsGrounded { get; set; }
        public bool IsCrouching { get; set; }

        void Awake()
        {
            Controller = GetComponent<CharacterController>();
            PlayerId = System.Guid.NewGuid().ToString();
        }
    }
}
