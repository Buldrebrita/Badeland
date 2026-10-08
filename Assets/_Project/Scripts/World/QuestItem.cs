using System.Collections.Generic;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// An object that belongs on a particular altar (a checkpoint). Put it on a thing that also has a
    /// <see cref="Carryable"/>; the altar shows a carving of the item it wants.
    /// </summary>
    public class QuestItem : MonoBehaviour
    {
        static readonly List<QuestItem> AllItems = new List<QuestItem>();
        public static IReadOnlyList<QuestItem> All => AllItems;

        public string itemId = "key";

        void Awake() => AllItems.Add(this);
        void OnDestroy() => AllItems.Remove(this);
    }
}
