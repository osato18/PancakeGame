using System.Collections;
using UnityEngine;

public class SmokeDelete : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SmokeDie());
    }

    IEnumerator SmokeDie()
    {
        yield return new WaitForSeconds(4f);
        Destroy(gameObject);
    }
    
}
