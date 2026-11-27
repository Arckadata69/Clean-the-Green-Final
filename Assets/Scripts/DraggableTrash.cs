using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class DraggableTrashPiece : MonoBehaviour
{
    private TrashNSortaController _controller;
    private Transform _spawnParent;
    private SpriteRenderer _spriteRenderer;
    private bool _isDragging;

    public TrashItemDefinition Data { get; private set; }

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    public void Initialize(TrashNSortaController controller, TrashItemDefinition data, Transform spawnParent)
    {
        _controller = controller;
        Data = data;
        _spawnParent = spawnParent;

        if (_spriteRenderer != null && data != null)
            _spriteRenderer.sprite = data.icon;

        ReturnToSpawn();
    }

    public void ReturnToSpawn()
    {
        _isDragging = false;
        if (_spawnParent != null)
        {
            transform.SetParent(_spawnParent, false);
            transform.localPosition = Vector3.zero;
        }
    }

    private void OnMouseDown()
    {
        if (_controller == null) return;
        _isDragging = true;
        Vector2 screenPos = Input.mousePosition;
        transform.position = _controller.ScreenToWorld(screenPos);
        _controller.SetDraggingVisual(true, screenPos);
    }

    private void OnMouseDrag()
    {
        if (!_isDragging || _controller == null) return;
        Vector2 screenPos = Input.mousePosition;
        transform.position = _controller.ScreenToWorld(screenPos);
        _controller.SetDraggingVisual(true, screenPos);
    }

    private void OnMouseUp()
    {
        if (!_isDragging) return;
        _isDragging = false;
        if (_controller != null)
            _controller.EvaluateDrop(this, Input.mousePosition);
    }
}