using UnityEngine;

namespace BrickStacker
{
    public class FloatyBrick : MonoBehaviour
    {
        public float Speed = 0.25f;
        float seed;

        void Awake()
        {
            seed = UnityEngine.Random.Range(0f, 10f);
        }

        void Update()
        {
            transform.position += Vector3.up * Mathf.Sin(Time.time * Speed + seed) * Time.deltaTime * 0.24f;
            transform.Rotate(0, 0, Speed * 12f * Time.deltaTime);
        }
    }

}
