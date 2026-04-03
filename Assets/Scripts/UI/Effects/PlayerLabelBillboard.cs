using UnityEngine;
using TMPro;

public class PlayerLabelBillboard : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private string labelText = "Player";

    private Transform _target;
    private TextMeshPro _text;

    public void Initialize(Transform target, string newLabel)
    {
        _target = target;
        labelText = newLabel;
    }

    private void Awake()
    {
        _text = GetComponent<TextMeshPro>();
        if (_text != null)
        {
            _text.text = labelText;
            _text.alignment = TextAlignmentOptions.Center;
        }
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        transform.position = _target.position + worldOffset;

        if (_text != null && _text.text != labelText)
            _text.text = labelText;
    }
}