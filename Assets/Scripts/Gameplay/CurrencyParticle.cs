using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class CurrencyParticle : MonoBehaviour
{
    public string anchorName;
    public float moveSpeed;
    public float arcHeight;
    public Transform animateTransform;


    private void OnEnable()
    {
        animateTransform.localPosition = Vector3.zero;
        StartCoroutine(MoveToUI());
    }

    private IEnumerator MoveToUI()
    {
        var targetPosition = Camera.main.ScreenToWorldPoint(GameObject.Find(anchorName).transform.position);
        var pos = targetPosition;
        pos.z = 0f;

        Vector3 objectPos = transform.position;
        pos.x -= objectPos.x;
        pos.y -= objectPos.y;
        
        float angle = Mathf.Atan2(pos.y, pos.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        var dist = Vector2.Distance(transform.position, targetPosition);
        int arc = (int)Random.Range(-arcHeight, arcHeight);
        while (Vector2.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            
            var progress = 1-(Vector2.Distance(transform.position, targetPosition) / dist);
            var height = arc * 4f * (progress - 0.5f) * (progress - 0.5f);
            animateTransform.localPosition = new Vector3(0, arc - height, 0);
            
            yield return new WaitForEndOfFrame();
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        SystemEventManager.Send(SystemEventManager.GameEvent.CurrencyAdded, 1);
    }
}