#pragma warning disable 649

using UnityEngine;
using Watermelon.SkinStore;

namespace Watermelon
{
    [CreateAssetMenu(menuName = "Level Database/Obstacle", fileName = "Obstacle")]
    public class Obstacle : ScriptableObject
    {
        [SerializeField] int shopIndex;
        [SerializeField] private FieldElement fieldElement;

        public Vector2Int Size => fieldElement.Size;
        public GameObject Prefab => SkinStoreController.GetSelectedPrefab(SkinTab.Environment).GetComponent<EnvironmentProductBehavior>().GetObstacleObject(shopIndex);
    }
}