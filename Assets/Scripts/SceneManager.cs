using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public Animator uiAnimator;

    public static SceneManager Instance;
    private static readonly int Load = Animator.StringToHash("Load");

    private void Awake()
    {
        if (Instance != null) Destroy(Instance.gameObject);
        Instance = this;
    }

    public void LoadScene(int index)
    {
        StopAllCoroutines();
        uiAnimator.SetTrigger(Load);
        UnityEngine.SceneManagement.SceneManager.LoadScene(index);
    }
}