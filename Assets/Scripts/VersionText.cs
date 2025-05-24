using TMPro;
using UnityEngine;

public class VersionText : MonoBehaviour
{
    private void Awake()
    {
       GetComponent<TMP_Text>().text = $"V{Application.version}";
    }
}