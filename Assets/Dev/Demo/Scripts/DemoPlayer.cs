using UnityEngine;
using UnityEngine.InputSystem;

namespace ScenarioForge.Demo
{
    /// <summary>Moves the demo player on the floor with WASD or the arrow keys.</summary>
    public sealed class DemoPlayer : MonoBehaviour
    {
        [SerializeField, Min(0)] float speed = 5f;
        [SerializeField, Min(0)] float areaHalfSize = 9f;

        Vector3 startPosition;

        void Awake()
        {
            startPosition = transform.position;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            var input = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (input == Vector2.zero)
                return;

            Vector3 move = new Vector3(input.x, 0f, input.y).normalized;
            Vector3 position = transform.position + move * (speed * Time.deltaTime);
            position.x = Mathf.Clamp(position.x, -areaHalfSize, areaHalfSize);
            position.z = Mathf.Clamp(position.z, -areaHalfSize, areaHalfSize);
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(move));
        }

        /// <summary>Moves the player back to where the scene started.</summary>
        public void ResetDemo()
        {
            transform.SetPositionAndRotation(startPosition, Quaternion.identity);
        }
    }
}
