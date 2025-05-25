using System;
using TMPro;
using UnityEngine;

public class GridCellDebugger : MonoBehaviour
{
    public TMP_Text valueText;
    public TMP_Text matValueText;
    public BoardObject bo;
    private void Update()
    {
        valueText.gameObject.SetActive(bo !=null);
        matValueText.gameObject.SetActive(bo != null);
        
        if (bo == null) return;
        
        valueText.text = $"V:{bo.GetValue()}";
        matValueText.text = $"MV:{bo.GetMaterialValue()}";
    }
}
