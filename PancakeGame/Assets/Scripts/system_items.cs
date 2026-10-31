using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class system_items : MonoBehaviour
{
    public system_scores scoresSystem;
    [SerializeField] private ItemsType itemType;
    public Collider p_col;
    private Items item;

    public system_playerDevice p_DeviceSys;

    [SerializeField] private float speed;
    private float speed_coe = -1.0f;
    private bool resolved;
    private bool tutorialItem;
    private float travelSign;
    private float tutorialAge;
    public bool Resolved => resolved;
    public System.Action<bool> TutorialResolved;

    public void ConfigureTutorial()
    {
        tutorialItem = true;
        travelSign = Mathf.Sign(p_col.bounds.center.z - transform.position.z);
    }

    public UIManager uiManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        item.col = gameObject.GetComponent<Collider>();
        item.rb = gameObject.GetComponent<Rigidbody>();
        item.rb.AddForce(0.0f, 0.0f, speed_coe * speed, ForceMode.VelocityChange);
        item.type = itemType;
        StartCoroutine(Destroy());
    }

    // Update is called once per frame
    void Update()
    {
        if (!tutorialItem || resolved) return;
        tutorialAge += Time.deltaTime;
        // A miss is recorded only after crossing behind the player's collision plane.
        if ((transform.position.z - p_col.bounds.center.z) * travelSign > 1f)
        {
            resolved = true;
            TutorialResolved?.Invoke(false);
            Destroy(gameObject);
        }
        else if (tutorialAge > 20f)
        {
            // Timeout/cleanup is not an avoidance success.
            resolved = true;
            Destroy(gameObject);
        }
    }

    void Scoring(ref Items items)
    {
        if (items.type.Equals(ItemsType.good))
        {
            scoresSystem.AddScore(100);
            if(!p_DeviceSys.AddFlagCheck((int)(item.type) + 1))
            {
                uiManager._currentItemNums[(int)items.type]++;
                UIManager.UpdateItemUI(items.type, uiManager._currentItemNums[(int)items.type]);
            }
        }
        else if (items.type.Equals(ItemsType.bad))
        {
            scoresSystem.AddScore(-100);
        }
    } 

    public void speedChange(float coe)
    {
        speed_coe = coe;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!resolved && other.Equals(p_col))
        {
            resolved = true;
            Scoring(ref item);
            if (tutorialItem) TutorialResolved?.Invoke(true);
            Destroy(gameObject);
        }
    }
    IEnumerator Destroy()
    {
        yield return new WaitForSeconds(5000f);
        Destroy(gameObject);
    }
}

public struct Items
{
    public Collider col;
    public Rigidbody rb;
    public ItemsType type;
}

public enum ItemsType
{
    good=0,
    bad
}
