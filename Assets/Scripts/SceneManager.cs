using UnityEngine;

public class SceneManager : MonoBehaviour
{
    private static readonly int Load = Animator.StringToHash("Load");
    public Animator fadeAnimator;

    public void LoadScene(int index)
    {
        fadeAnimator.SetTrigger(Load);
        UnityEngine.SceneManagement.SceneManager.LoadScene(index);
    }
}