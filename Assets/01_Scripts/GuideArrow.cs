using TMPro;
using UnityEngine;

/// <summary>
/// 목적지 안내 화살표: 목적지를 고르면 몸 앞에 3D 화살표가 떠서 목적지 방향을 가리키고 남은 거리를 보여줌.
/// 가까워지면(arriveDistance 이내) 화살표가 사라지고 "도착!" 문구를 잠깐 보여줌.
/// </summary>
public class GuideArrow : MonoBehaviour
{
    [System.Serializable]
    public class Target
    {
        public string name;
        public Transform point;
    }

    public Target[] targets;
    public Transform arrow;          // 화살표 모델 (앞이 +Z)
    public Transform labelRoot;      // 거리 표시 월드 캔버스
    public TMP_Text label;
    public AudioSource sfx;
    public float arriveDistance = 8f;
    public float forwardOffset = 1.2f;   // 머리 앞쪽 거리
    public float downOffset = 0.3f;      // 눈높이보다 아래

    private int current = -1;
    private bool arrived;
    private float hideAt;

    public bool Active => current >= 0;

    private void Start() => SetVisible(false, false);

    public void SetTarget(int index)
    {
        if (index < 0 || index >= targets.Length || targets[index].point == null) return;
        current = index;
        arrived = false;
        SetVisible(true, true);
    }

    public void Stop()
    {
        current = -1;
        arrived = false;
        SetVisible(false, false);
    }

    private void SetVisible(bool arrowOn, bool labelOn)
    {
        arrow.gameObject.SetActive(arrowOn);
        labelRoot.gameObject.SetActive(labelOn);
    }

    private void LateUpdate()
    {
        if (current < 0) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Target target = targets[current];
        Vector3 flatToTarget = target.point.position - cam.transform.position;
        flatToTarget.y = 0f;
        float distance = flatToTarget.magnitude;

        Vector3 flatForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.01f) flatForward = Vector3.forward;
        Vector3 anchor = cam.transform.position + flatForward * forwardOffset + Vector3.down * downOffset;

        if (!arrived && distance <= arriveDistance)
        {
            arrived = true;
            hideAt = Time.time + 3f;
            arrow.gameObject.SetActive(false);
            label.text = $"도착! {target.name}에 왔어요";
            if (sfx != null && sfx.clip != null)
            {
                sfx.pitch = 1.5f;
                sfx.PlayOneShot(sfx.clip);
            }
        }

        if (arrived)
        {
            if (Time.time >= hideAt) Stop();
        }
        else
        {
            arrow.position = anchor + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.02f);
            arrow.rotation = Quaternion.LookRotation(flatToTarget.normalized);
            label.text = $"{target.name}까지 {Mathf.RoundToInt(distance)}m";
        }

        // 글자판은 화살표 위에서 보는 사람을 향함
        labelRoot.position = anchor + Vector3.up * 0.3f;
        labelRoot.rotation = Quaternion.LookRotation(labelRoot.position - cam.transform.position);
    }
}
