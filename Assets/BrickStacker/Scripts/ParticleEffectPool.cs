using System.Collections.Generic;
using UnityEngine;

namespace BrickStacker
{
    // Pool cho MỘT prefab hiệu ứng hạt. Hiệu ứng bắn liên tục suốt trận nên không được
    // Instantiate/Destroy mỗi lần — giữ một số instance cố định rồi xoay vòng.
    //
    // KHÔNG dựa vào IsAlive để biết instance nào rảnh: root của các prefab VFX này để loop vô hạn,
    // nên IsAlive luôn true. Pool cũ vì thế không bao giờ tái dùng — mỗi đòn lại Instantiate thêm
    // một bản và để lại một hệ hạt chạy mãi. Xoay vòng thì bộ nhớ có trần và không phụ thuộc cách
    // prefab được dựng.
    public sealed class ParticleEffectPool
    {
        readonly ParticleSystem prefab;
        readonly Transform parent;
        readonly int capacity;
        readonly List<ParticleSystem> instances = new List<ParticleSystem>();
        int next;

        // capacity = số hiệu ứng được phép chồng lên nhau cùng lúc; đòn thứ capacity+1 sẽ lấy lại
        // bản cũ nhất.
        public ParticleEffectPool(ParticleSystem prefab, Transform parent, int capacity)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.capacity = Mathf.Max(1, capacity);
        }

        public bool HasPrefab => prefab != null;

        public ParticleSystem Get()
        {
            if (prefab == null)
            {
                return null;
            }

            if (instances.Count < capacity)
            {
                ParticleSystem spawned = Object.Instantiate(prefab, parent);
                instances.Add(spawned);
                return spawned;
            }

            int slot = next;
            next = (next + 1) % instances.Count;

            // Instance có thể đã bị huỷ theo scene — sinh lại đúng ô đó.
            if (instances[slot] == null)
            {
                instances[slot] = Object.Instantiate(prefab, parent);
            }
            return instances[slot];
        }
    }
}
