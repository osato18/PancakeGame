using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class system_itemsSpawner : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private system_scores scoresSystem;
    [SerializeField] private GameObject[] itemObj;
    [SerializeField] private Transform[] spawnTrans = new Transform[2];
    [SerializeField] private Collider p_col;
    [SerializeField] private float minSpanTime = 3f;
    [SerializeField] private float rangeTime = 5f;
    public List<system_items> itemsSystem = new List<system_items>();
    public float SpawnX => spawnTrans[0].position.x;
    public bool TutorialReady => itemObj != null && itemObj.Length >= 2 &&
        itemObj[0] != null && itemObj[1] != null && spawnTrans != null &&
        spawnTrans.Length >= 2 && spawnTrans[0] != null && spawnTrans[1] != null && p_col != null;

    void Start() { StartCoroutine(SpawnSpan()); }

    IEnumerator SpawnSpan()
    {
        while (true)
        {
            yield return new WaitForSeconds(minSpanTime + Random.Range(0f, rangeTime));
            if (system_tutorial.IsRunning) continue;
            SpawnObj(itemObj[Random.Range(0, itemObj.Length)], Random.Range(0, spawnTrans.Length));
        }
    }

    system_items SpawnObj(GameObject prefab, int heightIndex)
    {
        Transform point = spawnTrans[heightIndex];
        GameObject clone = Instantiate(prefab, point.position, point.rotation);
        system_items item = clone.GetComponent<system_items>();
        if (item == null) { Destroy(clone); return null; }
        item.p_col = p_col;
        item.scoresSystem = scoresSystem;
        item.uiManager = uiManager;
        itemsSystem.RemoveAll(entry => entry == null);
        itemsSystem.Add(item);
        return item;
    }

    public system_items SpawnTutorialItem(ItemsType type, bool airborne)
    {
        if (!TutorialReady) return null;
        int low = spawnTrans[0].position.y <= spawnTrans[1].position.y ? 0 : 1;
        int height = airborne ? 1 - low : low;
        return SpawnObj(itemObj[(int)type], height);
    }

    public void ClearItems()
    {
        foreach (system_items item in itemsSystem)
            if (item != null) { item.gameObject.SetActive(false); Destroy(item.gameObject); }
        itemsSystem.Clear();
    }

    public void lineItemSpeedChange(float coe)
    {
        itemsSystem.RemoveAll(entry => entry == null);
        foreach (system_items item in itemsSystem) item.speedChange(coe);
    }
}
