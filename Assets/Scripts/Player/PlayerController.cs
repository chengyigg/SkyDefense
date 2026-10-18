using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    [Header("移动")]
    public float moveSpeed = 20f;
    public float arriveThreshold = 0.1f;
    public float rotateSpeed = 10f;

    [Header("地图边界 (地面150x100)")]
    public float mapHalfSizeX = 70f;
    public float mapHalfSizeZ = 45f;

    [Header("网格设置")]
    public float cellSize = 5f;
    public GameObject buildPreviewPrefab;

    [Header("状态")]
    public bool canMove = true;
    public bool canShowPreview = true;

    public bool IsMouseOnRoad { get; private set; } = false;

    private Vector3 _targetPosition;
    private bool _hasTarget = false;
    private Vector3 _currentGridCenter;
    private bool _isMouseOnGround = false;

    private Vector3 _lastMousePosition;

    // 【表现】建塔预览渲染器与属性块
    private MeshRenderer _previewRenderer;
    private MaterialPropertyBlock _previewMpb;

    [Header("出生点")]
    public Vector3 spawnPosClient0 = new Vector3(-30, 10, 20);
    public Vector3 spawnPosClient1 = new Vector3(30, 10, 20);

    void Start()
    {
        _targetPosition = transform.position;
        _lastMousePosition = Input.mousePosition;
        _previewMpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (!IsOwner) return;
        if (!canMove) return;

        UpdateMouseRay();
        UpdateMovement();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Vector3 spawn = OwnerClientId == 0 ? spawnPosClient0 : spawnPosClient1;
            transform.position = spawn;

            if (buildPreviewPrefab == null)
            {
                var preview = GameObject.Find("BuildPreview");
                if (preview != null)
                {
                    buildPreviewPrefab = preview;
                    buildPreviewPrefab.SetActive(false);
                }
            }

            if (buildPreviewPrefab != null)
            {
                _previewRenderer = buildPreviewPrefab.GetComponentInChildren<MeshRenderer>();
            }

            if (BuildManager.Instance != null)
                BuildManager.Instance.player = this;
        }
    }

    private Enemy GetEnemyUnderMouse()
    {
        if (EnemyManager.Instance == null) return null;

        float bestDist = 80f;
        Enemy best = null;

        var all = EnemyManager.Instance.GetAllEnemies();
        foreach (var e in all)
        {
            if (e == null || e.IsDead) continue;

            Vector3 sp = Camera.main.WorldToScreenPoint(e.transform.position);
            if (sp.z < 0f) continue;

            float d = Vector2.Distance(new Vector2(sp.x, sp.y), Input.mousePosition);
            if (d < bestDist)
            {
                bestDist = d;
                best = e;
            }
        }
        return best;
    }

    private void UpdateMouseRay()
    {
        Vector3 mousePos = Input.mousePosition;
        bool mouseMoved = (mousePos - _lastMousePosition).sqrMagnitude > 0.01f;
        _lastMousePosition = mousePos;

        if (mouseMoved)
        {
            UpdateMouseHover();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (!_isMouseOnGround) return;
            HandleClick();
        }
    }

    private void UpdateMouseHover()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (buildPreviewPrefab != null) buildPreviewPrefab.SetActive(false);
            IsMouseOnRoad = false;
            _isMouseOnGround = false;
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            Vector3 point = hit.point;
            float gridX = Mathf.RoundToInt(point.x / cellSize) * cellSize;
            float gridZ = Mathf.RoundToInt(point.z / cellSize) * cellSize;
            _currentGridCenter = new Vector3(gridX, 0.05f, gridZ);

            _currentGridCenter.x = Mathf.Clamp(_currentGridCenter.x, -mapHalfSizeX, mapHalfSizeX);
            _currentGridCenter.z = Mathf.Clamp(_currentGridCenter.z, -mapHalfSizeZ, mapHalfSizeZ);

            _isMouseOnGround = true;

            int roadLayer = LayerMask.NameToLayer("Road");
            Vector3 checkCenter = _currentGridCenter + Vector3.up * 1f;
            Vector3 halfExtents = new Vector3(2.4f, 1f, 2.4f);
            IsMouseOnRoad = Physics.CheckBox(checkCenter, halfExtents, Quaternion.identity, 1 << roadLayer);

            bool hasTower = false;
            if (BuildManager.Instance != null)
            {
                var key = BuildManager.Instance.WorldToGrid(_currentGridCenter);
                var existing = BuildManager.Instance.GetTowerAt(key);
                if (existing != null && existing.IsBuilt) hasTower = true;
            }

            // 【表现】预览红绿变色
            if (canShowPreview && buildPreviewPrefab != null)
            {
                buildPreviewPrefab.SetActive(true);
                buildPreviewPrefab.transform.position = _currentGridCenter;

                if (!IsMouseOnRoad && !hasTower)
                    SetPreviewColor(new Color(0f, 1f, 0f, 0.5f)); // 合法：半透明绿色
                else
                    SetPreviewColor(new Color(1f, 0f, 0f, 0.5f)); // 非法：半透明红色
            }
            else if (buildPreviewPrefab != null)
            {
                buildPreviewPrefab.SetActive(false);
            }
        }
        else
        {
            _isMouseOnGround = false;
            IsMouseOnRoad = false;
            if (buildPreviewPrefab != null) buildPreviewPrefab.SetActive(false);
        }
    }

    private void SetPreviewColor(Color color)
    {
        if (_previewRenderer == null) return;

        _previewRenderer.GetPropertyBlock(_previewMpb);
        _previewMpb.SetColor("_BaseColor", color);
        _previewMpb.SetColor("_Color", color);
        _previewRenderer.SetPropertyBlock(_previewMpb);
    }

    private void HandleClick()
    {
        if (BuildManager.Instance != null &&
            (BuildManager.Instance.IsMenuOpen || BuildManager.Instance.IsUpgradePanelOpen))
            return;

        if (BuildManager.Instance != null)
        {
            var key = BuildManager.Instance.WorldToGrid(_currentGridCenter);
            var tower = BuildManager.Instance.GetTowerAt(key);
            if (tower != null && tower.IsBuilt)
            {
                if (TowerRangeIndicator.Instance != null)
                    TowerRangeIndicator.Instance.Show(tower.transform.position, tower.GetCurrentRange());
                return;
            }
        }

        if (LockOnManager.Instance != null)
        {
            Enemy clicked = GetEnemyUnderMouse();
            if (clicked != null)
            {
                LockOnManager.Instance.RequestLock(clicked);
                return;
            }
        }

        if (TowerRangeIndicator.Instance != null)
            TowerRangeIndicator.Instance.Hide();

        MoveTo(_currentGridCenter);
    }

    public void MoveTo(Vector3 targetPos)
    {
        SoundManager.Instance?.Play("玩家移动");
        Vector3 target = targetPos;
        target.y = transform.position.y;
        target.x = Mathf.Clamp(target.x, -mapHalfSizeX, mapHalfSizeX);
        target.z = Mathf.Clamp(target.z, -mapHalfSizeZ, mapHalfSizeZ);
        _targetPosition = target;
        _hasTarget = true;
    }

    private void UpdateMovement()
    {
        if (_hasTarget)
        {
            Vector3 dir = _targetPosition - transform.position;
            dir.y = 0f;

            if (dir.magnitude <= arriveThreshold)
            {
                _hasTarget = false;
            }
            else
            {
                Vector3 moveDir = dir.normalized;
                transform.position += moveDir * moveSpeed * Time.deltaTime;
                if (moveDir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
                }
            }
        }
    }
}