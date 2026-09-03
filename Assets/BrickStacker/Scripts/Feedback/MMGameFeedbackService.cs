using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace BrickStacker
{
    // Phat hieu ung Feel theo moc gameplay.
    // Binding gan san trong Inspector cua prefab, cache 1 lan vao Dictionary luc Awake
    // nen call site khong ton lookup. Them moc moi chi can them 1 dong trong Inspector,
    // khong phai sua class nay.
    public sealed class MMGameFeedbackService : MonoBehaviour, IGameFeedbackService
    {
        [SerializeField] GameFeedbackBinding[] bindings = new GameFeedbackBinding[0];

        readonly Dictionary<GameFeedbackId, MMF_Player> players = new Dictionary<GameFeedbackId, MMF_Player>();

        void Awake()
        {
            BuildLookup();
        }

        void BuildLookup()
        {
            players.Clear();
            if (bindings == null)
            {
                return;
            }

            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding == null || binding.Player == null || binding.Id == GameFeedbackId.None)
                {
                    continue;
                }

                if (players.ContainsKey(binding.Id))
                {
                    Debug.LogWarning("BLOCKFALL feedback bi gan trung: " + binding.Id, this);
                    continue;
                }

                players.Add(binding.Id, binding.Player);
            }
        }

        public void Play(GameFeedbackId id)
        {
            Play(id, transform.position);
        }

        public void Play(GameFeedbackId id, Vector3 position, float intensity = 1f)
        {
            if (!players.TryGetValue(id, out var player) || player == null)
            {
                return;
            }

            player.PlayFeedbacks(position, intensity);
        }
    }
}
