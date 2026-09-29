using UnityEngine;

[CreateAssetMenu(fileName = "MonsterData", menuName = "Deploy/MonsterData")]
public class MonsterDataSO : ScriptableObject
{
    public float maxHp;
    public float speed;
    public Sprite sprite;
    public int rewardGold;
}
