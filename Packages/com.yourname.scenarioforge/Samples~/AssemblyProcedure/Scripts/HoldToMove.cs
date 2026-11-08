using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScenarioForge.Samples
{
    /// <summary>While the button is held down (and interactable), moves a transform towards a destination.</summary>
    [RequireComponent(typeof(Selectable))]
    public sealed class HoldToMove : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] Transform target;
        [SerializeField] Transform destination;
        [SerializeField, Min(0)] float speed = 2f;

        Selectable selectable;
        bool held;

        void Awake() => selectable = GetComponent<Selectable>();

        public void OnPointerDown(PointerEventData eventData) => held = true;

        public void OnPointerUp(PointerEventData eventData) => held = false;

        public void OnPointerExit(PointerEventData eventData) => held = false;

        void Update()
        {
            if (!held || !selectable.IsInteractable() || target == null || destination == null)
                return;

            Vector3 goal = destination.position;
            goal.y = target.position.y;
            target.position = Vector3.MoveTowards(target.position, goal, speed * Time.deltaTime);
        }
    }
}
