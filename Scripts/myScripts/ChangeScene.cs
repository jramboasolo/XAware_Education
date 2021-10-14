using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


// Class used to manage the change of scene in the application
public class ChangeScene : MonoBehaviour
{

    //Name of the different scenes in the application
    [SerializeField]
    private string nameScene1,nameScene2;
    AsyncOperation asyncLoad;

    // Start is called before the first frame update
    private void Start()
    {
        asyncLoad = new AsyncOperation();
    }

    //Function called to load the scene1
    public void LoadScene1()
    {
        StartCoroutine(LoadAsyncScene(nameScene1));
    }

    //Function called to load the scene2
    public void LoadScene2()
    {
        SceneManager.LoadScene(nameScene2);
    }

    //Function called to exit the application
    public void QuitApplication()
    {
        Application.Quit();
    }

    //Function used with the WaitUntil to wait until the progress is done and the scene is ready
    private bool IsDone()
    {
        return (asyncLoad.progress >= 0.9f);
    }

    //Function called by the coroutine
    //Use to smooth the change
    private IEnumerator LoadAsyncScene(string nameScene)
    {
        asyncLoad = SceneManager.LoadSceneAsync(nameScene);
        asyncLoad.allowSceneActivation = false;
        yield return new WaitUntil(IsDone);
        asyncLoad.allowSceneActivation = true;
    }

}
