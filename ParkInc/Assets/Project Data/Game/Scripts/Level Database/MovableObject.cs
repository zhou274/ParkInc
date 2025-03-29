#pragma warning disable 649

using UnityEngine;
using Watermelon.SkinStore;

namespace Watermelon
{
    [CreateAssetMenu(menuName = "Level Database/Movable Object", fileName = "Movable Object")]
    public class MovableObject : ScriptableObject
    {
        [SerializeField] int shopIndex;
        [SerializeField] private FieldElement fieldElement;

        public GameObject Prefab => SkinStoreController.GetSelectedPrefab(SkinTab.Cars).GetComponent<CarProductBehavior>().GetMovableObject(shopIndex);
        public Vector2Int Size => fieldElement.Size;
    }
}