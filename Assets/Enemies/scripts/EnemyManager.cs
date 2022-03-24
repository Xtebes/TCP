using UnityEngine;
public class EnemyManager : MonoBehaviour
{
    public Transform[] enemyPools;
    public GameObject[] enemyPrefabs;
    public System.Action<Enemy> onEnemyInstantiated;
    public static System.Action<Vector3> onSpawnEnemy;
    [SerializeField]
    private float maxEnemyAmount;
    public Enemy TrySpawnEnemy(int enemyIndex, Vector3 position)
    {
        int enemyAmount = 0;
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            enemyAmount += enemyPools[i].GetChild(0).childCount;
        }
        if (enemyAmount >= maxEnemyAmount)
        {
            return null;
        }
        Transform enemyTransform;
        Enemy enemy;
        if (enemyPools[enemyIndex].GetChild(1).childCount > 0)
        {
            enemyTransform = enemyPools[enemyIndex].GetChild(1).GetChild(0);
            enemyTransform.gameObject.SetActive(true);
        }
        else
        {
            enemyTransform = Instantiate(enemyPrefabs[enemyIndex]).transform;
            enemy = enemyTransform.GetComponent<Enemy>();
            onEnemyInstantiated.Invoke(enemy);
        }
        enemy = enemyTransform.GetComponent<Enemy>();
        enemy.onDestroy = () => DestroyEnemy(enemy);
        enemyTransform.GetComponent<Rigidbody>().velocity = Vector3.zero;
        enemyTransform.parent = enemyPools[enemyIndex].GetChild(0);
        enemyTransform.position = position;
        return enemy;
    }
    public void DestroyEnemy(Enemy enemy)
    {
        enemy.transform.gameObject.SetActive(false);
        enemy.transform.parent = enemyPools[enemy.id].GetChild(1);
    }
}
