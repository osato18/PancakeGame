using UnityEngine;
using UnityEngine.SceneManagement;

public class system_Scene : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void EventSceneManager(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

}
