using UnityEngine;

namespace ScenarioForge.Samples
{
    /// <summary>
    /// Moves the sample's parts and cart. The buttons call these methods; the scenario never does.
    /// The station only shows what happened, so the step order lives entirely in the scenario asset.
    /// </summary>
    public sealed class AssemblyStation : MonoBehaviour
    {
        [SerializeField] Transform cart;
        [SerializeField] Transform partA;
        [SerializeField] Transform partB;
        [SerializeField] GameObject dockLight;

        Pose cartStart;
        Pose partAStart;
        Pose partBStart;
        Transform partsParent;

        void Awake()
        {
            cartStart = new Pose(cart.position, cart.rotation);
            partAStart = new Pose(partA.position, partA.rotation);
            partBStart = new Pose(partB.position, partB.rotation);
            partsParent = partA.parent;
        }

        /// <summary>Puts part A on the cart, next to the base.</summary>
        public void PickPartA() => Place(partA, new Vector3(-0.55f, 0.45f, -0.35f));

        /// <summary>Puts part B on the cart, next to the base.</summary>
        public void PickPartB() => Place(partB, new Vector3(0.55f, 0.45f, -0.35f));

        /// <summary>Stacks both parts on the base.</summary>
        public void AttachParts()
        {
            Place(partA, new Vector3(0f, 0.65f, 0f));
            Place(partB, new Vector3(0f, 1.15f, 0f));
        }

        /// <summary>Lights the dock. Called by a signal listener when the scenario sends <c>assembly_delivered</c>.</summary>
        public void LightDock() => dockLight.SetActive(true);

        /// <summary>Puts everything back where the scene started.</summary>
        public void ResetStation()
        {
            cart.SetPositionAndRotation(cartStart.position, cartStart.rotation);
            Restore(partA, partAStart);
            Restore(partB, partBStart);
            dockLight.SetActive(false);
        }

        void Place(Transform part, Vector3 localPosition)
        {
            part.SetParent(cart, false);
            part.localPosition = localPosition;
            part.localRotation = Quaternion.identity;
        }

        void Restore(Transform part, Pose pose)
        {
            part.SetParent(partsParent, false);
            part.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }
}
