using TMPro;
using UnityEngine;

/// <summary>
/// Per-cell debug readout. Refreshed by its <see cref="GridCell"/> only while debug is on, rather
/// than from an Update that ran on every cell every frame regardless.
/// </summary>
public class GridCellDebugger : MonoBehaviour
{
    public TMP_Text valueText;
    public TMP_Text matValueText;
    public BoardObject bo;

    public void Refresh()
    {
        bool hasObject = bo != null;

        if (valueText.gameObject.activeSelf != hasObject) valueText.gameObject.SetActive(hasObject);
        if (matValueText.gameObject.activeSelf != hasObject) matValueText.gameObject.SetActive(hasObject);

        if (!hasObject) return;

        valueText.text = $"V:{bo.GetValue()}";
        matValueText.text = $"MV:{bo.GetMaterialValue()}";
    }
}
