using Managers;
using TMPro;
using UnityEngine;

namespace UI
{
    public class UpgradePanel : MonoBehaviour
    {
        public TMP_Text pointsText;

        private void Update()
        {
            pointsText.text = $"Upgrade Points: {PurchaseManager.GetCurrentUpgradePoints()}";
        }
    }
}