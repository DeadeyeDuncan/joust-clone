using System;
using UnityEngine;

namespace Joust.World
{
    /// <summary>
    /// Teleports an entity across the arena bounds and maintains a ghost copy
    /// near the seam, so the crossing is not visible as a pop.
    /// </summary>
    public class ScreenWrapPrototype : MonoBehaviour
    {
        [SerializeField] private float halfWidth = 16f;
        [SerializeField] private float ghostMargin = 4f;

        private Transform _ghost;

        public float HalfWidth => halfWidth;

        public void Configure(float arenaHalfWidth, float margin)
        {
            halfWidth = arenaHalfWidth;
            ghostMargin = margin;
        }

        /// <summary>
        /// Pure wrap arithmetic, separated from the scene so it can be tested
        /// without physics. Returns the x an entity should occupy.
        /// </summary>
        public static float WrapX(float x, float halfWidth)
        {
            var width = halfWidth * 2f;
            if (width <= 0f)
            {
                return x;
            }

            var shifted = x + halfWidth;
            var wrapped = shifted - width * Mathf.Floor(shifted / width);
            return wrapped - halfWidth;
        }

        private void Start()
        {
            _ghost = BuildVisualGhost(transform);
            _ghost.gameObject.SetActive(false);
        }

        /// <summary>
        /// Builds a visuals-only double by copying meshes, rather than cloning
        /// the rider and stripping it.
        ///
        /// Cloning and stripping does not work: Destroy is deferred to the end of
        /// the frame, so ordering the removals has no effect and Unity still
        /// tries to remove a component another one requires. Copying meshes also
        /// guarantees the ghost has no colliders, and a ghost with colliders can
        /// joust, be jousted and land on platforms, which turns a cosmetic double
        /// into a second player.
        /// </summary>
        private static Transform BuildVisualGhost(Transform source)
        {
            var ghost = new GameObject($"{source.name}_ghost").transform;
            ghost.position = source.position;
            ghost.rotation = source.rotation;
            ghost.localScale = source.localScale;

            foreach (var renderer in source.GetComponentsInChildren<MeshRenderer>())
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                var piece = new GameObject(renderer.name);
                piece.transform.SetParent(ghost, false);

                // Copy the world-relative placement, so nested model hierarchies
                // land in the right spot without replicating their parents.
                piece.transform.position = renderer.transform.position;
                piece.transform.rotation = renderer.transform.rotation;
                piece.transform.localScale = renderer.transform.lossyScale;

                piece.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                piece.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
            }

            return ghost;
        }

        private void LateUpdate()
        {
            var position = transform.position;
            var wrapped = WrapX(position.x, halfWidth);

            if (!Mathf.Approximately(wrapped, position.x))
            {
                position.x = wrapped;
                transform.position = position;
            }

            if (_ghost == null)
            {
                return;
            }

            var distanceToEdge = halfWidth - Mathf.Abs(position.x);
            if (distanceToEdge > ghostMargin)
            {
                _ghost.gameObject.SetActive(false);
                return;
            }

            _ghost.gameObject.SetActive(true);
            var offset = position.x > 0f ? -halfWidth * 2f : halfWidth * 2f;
            _ghost.position = new Vector3(position.x + offset, position.y, position.z);
            _ghost.rotation = transform.rotation;
        }
    }
}
