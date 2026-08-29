// SPIKE: throwaway, not carried into M1.
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
            var ghost = Instantiate(gameObject, transform.position, transform.rotation);
            ghost.name = $"{name}_ghost";

            foreach (var behaviour in ghost.GetComponents<MonoBehaviour>())
            {
                Destroy(behaviour);
            }

            var body = ghost.GetComponent<Rigidbody>();
            if (body != null)
            {
                Destroy(body);
            }

            var collider = ghost.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            _ghost = ghost.transform;
            _ghost.gameObject.SetActive(false);
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
