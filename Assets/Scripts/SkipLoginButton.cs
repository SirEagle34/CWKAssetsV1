
using UnityEngine;
using UnityEngine.SceneManagement;

public class SkipLoginButton : MonoBehaviour
{
    public void SkipLogin()
    {
        PlayerPrefs.SetInt("SkipLogin", 1);
        PlayerPrefs.Save();

        SceneFlowManager.Instance.LoadBootVideoSceneDirect();
    }
}