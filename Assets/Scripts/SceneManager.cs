using System;
using System.Collections;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    private static readonly int Load = Animator.StringToHash("Load");
    public Animator fadeAnimator;

    public static SceneManager Instance;

    private void Awake()
    {
        if (Instance != null) Destroy(Instance.gameObject);

        Instance = this;
    }

    public void LoadScene(int index)
    {
        StopAllCoroutines();
        fadeAnimator.SetTrigger(Load);
        UnityEngine.SceneManagement.SceneManager.LoadScene(index);
    }
}