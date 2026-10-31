using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class system_items : MonoBehaviour
{
    public system_scores scoresSystem;
    [SerializeField] private ItemsType itemType;
    public Collider p_col;
    private Items item;

    [SerializeField] private float speed;
    private float speed_coe = -1.0f;

    

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
        
    }

    void Scoring(ref Items items)
    {
        
        if (items.type.Equals(ItemsType.good))
        {
            scoresSystem.AddScore(100);
            uiManager._currentItemNums[(int)items.type]++;
            UIManager.UpdateItemUI(items.type, uiManager._currentItemNums[(int)items.type]);
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
        if (other.Equals(p_col))
        {
            Scoring(ref item);
            
            Vector3 pos=this.transform.position;
            //バフエフェクト生成と効果音再生
            if (item.type.Equals(ItemsType.good))
            {
                EffectManager.PlayEffect("ItemGetEffect_Sphere005_Particle_Glow",pos);
                AudioManager.PlaySE("BuffGet");
            }
            //デバフ時のパーティクル生成と効果音再生
            else if (item.type.Equals(ItemsType.bad))
            {
                EffectManager.PlayEffect("DebuffEffect",pos);
                AudioManager.PlaySE("Debuffitem");
            }
           
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