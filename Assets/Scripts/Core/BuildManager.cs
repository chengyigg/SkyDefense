using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Unity.Netcode;

public class BuildManager : NetworkBehaviour
{
    public static BuildManager Instance;

    public TowerData[] towerDatas;
    public PlayerController player;
    public UIPanelAnimator buildMenuAnimator;
    public UpgradePanel upgradePanel;

    private Vector3 _pendingBuildPosition;
    private bool _isFlyingToBuild = false;

    private Dictionary<Vector2Int, Tower> _towerGrid = new Dictionary<Vector2Int, Tower>();

    private enum PendingAction { None, OpenBuildMenu, OpenUpgrade }
    private PendingAction _pendingAction = PendingAction.None;
    private Tower _pendingTower = null;

    public bool IsMenuOpen => buildMenuAnimator != null && buildMenuAnimator.gameObject.activeInHierarchy;
    public bool IsUpgradePanelOpen => upgradePanel != null && upgradePanel.gameObject.activeInHierarchy;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            bool closed = false;
            if (IsMenuOpen) { buildMenuAnimator.Hide(); closed = true; }
            if (IsUpgradePanelOpen) { upgradePanel.Hide(); closed = true; }
            if (closed && player != null) player.canShowPreview = true;
        }

        if (Input.GetMouseButtonDown(1) && !_isFlyingToBuild && player != null)
        {
            if (player.IsMouseOnRoad) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                float gridX = Mathf.RoundToInt(hit.point.x / player.cellSize) * player.cellSize;
                float gridZ = Mathf.RoundToInt(hit.point.z / player.cellSize) * player.cellSize;
                _pendingBuildPosition = new Vector3(gridX, 0, gridZ);

                Vector2Int key = WorldToGrid(_pendingBuildPosition);
                var existingTower = GetTowerAt(key);
                if (existingTower != null && existingTower.IsBuilt)
                {
                    _pendingAction = PendingAction.OpenUpgrade;
                    _pendingTower = existingTower;
                }
                else
                {
                    _pendingAction = PendingAction.OpenBuildMenu;
                    _pendingTower = null;
                }

                player.MoveTo(_pendingBuildPosition);
                if (TowerRangeIndicator.Instance != null) TowerRangeIndicator.Instance.Hide();
                player.canShowPreview = false;
                _isFlyingToBuild = true;

                if (IsMenuOpen) buildMenuAnimator.Hide();
                if (IsUpgradePanelOpen) upgradePanel.Hide();
            }
        }

        if (_isFlyingToBuild && player != null)
        {
            float dist = Vector3.Distance(
                new Vector3(player.transform.position.x, 0, player.transform.position.z),
                _pendingBuildPosition);

            if (dist < 6f)
            {
                _isFlyingToBuild = false;

                if (_pendingAction == PendingAction.OpenUpgrade && _pendingTower != null)
                    upgradePanel.Show(_pendingTower);
                else if (_pendingAction == PendingAction.OpenBuildMenu)
                    buildMenuAnimator.Show();

                _pendingAction = PendingAction.None;
                _pendingTower = null;
            }
        }
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        float cs = player != null ? player.cellSize : 5f;
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cs),
            Mathf.RoundToInt(worldPos.z / cs));
    }

    public Tower GetTowerAt(Vector2Int key)
    {
        _towerGrid.TryGetValue(key, out var t);
        return t;
    }

    public void RegisterTower(Vector2Int key, Tower t)
    {
        _towerGrid[key] = t;
    }
    public void UnregisterTower(Vector2Int key)
    {
        _towerGrid.Remove(key);
    }
    public void SelectTower(int index)
    {
        if (index < 0 || index >= towerDatas.Length) return;
        if (player == null) return;

        float gridX = Mathf.RoundToInt(player.transform.position.x / player.cellSize) * player.cellSize;
        float gridZ = Mathf.RoundToInt(player.transform.position.z / player.cellSize) * player.cellSize;
        Vector3 buildPos = new Vector3(gridX, 0, gridZ);

        if (NetworkManager.Singleton != null)
            RequestBuildServerRpc(index, buildPos);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestBuildServerRpc(int index, Vector3 buildPos, ServerRpcParams p = default)
    {
        if (index < 0 || index >= towerDatas.Length) return;

        var data = towerDatas[index];
        if (data == null) return;

        var senderPlayer = FindPlayerByClientId(p.Receive.SenderClientId);
        if (senderPlayer == null)
        {
            Debug.LogWarning($"[AntiCheat] 找不到 Client {p.Receive.SenderClientId} 的 PlayerController");
            return;
        }

        float cs = senderPlayer.cellSize;
        float snapX = Mathf.Round(buildPos.x / cs) * cs;
        float snapZ = Mathf.Round(buildPos.z / cs) * cs;
        if (Mathf.Abs(buildPos.x - snapX) > 0.01f || Mathf.Abs(buildPos.z - snapZ) > 0.01f)
        {
            Debug.LogWarning($"[AntiCheat] Client {p.Receive.SenderClientId} 建塔位置不在网格上: {buildPos}");
            return;
        }

        float distToPlayer = Vector3.Distance(
            new Vector3(senderPlayer.transform.position.x, 0, senderPlayer.transform.position.z),
            new Vector3(buildPos.x, 0, buildPos.z));
        if (distToPlayer > cs * 1.5f)
        {
            Debug.LogWarning($"[AntiCheat] Client {p.Receive.SenderClientId} 建塔距离玩家过远: {distToPlayer:F2} > {cs * 1.5f:F2}");
            return;
        }

        int roadLayer = LayerMask.NameToLayer("Road");
        Vector3 checkCenter = buildPos + Vector3.up * 1f;
        Vector3 halfExtents = new Vector3(2.4f, 1f, 2.4f);
        if (Physics.CheckBox(checkCenter, halfExtents, Quaternion.identity, 1 << roadLayer))
        {
            Debug.Log("路上不能建塔！");
            return;
        }

        Vector2Int key = WorldToGrid(buildPos);
        if (_towerGrid.ContainsKey(key) && _towerGrid[key] != null)
        {
            Debug.Log("这个格子已经有塔了！");
            return;
        }

        if (GameManager.Instance == null) return;
        if (!GameManager.Instance.SpendGold(data.cost))
        {
            Debug.Log("金币不足");
            return;
        }

        GameObject obj = Instantiate(data.towerPrefab, buildPos, Quaternion.identity);
        Tower tower = obj.GetComponent<Tower>();
        if (tower != null)
        {
            tower.data = data;
        }

        NetworkObject no = obj.GetComponent<NetworkObject>();
        if (no != null)
        {
            no.Spawn();

            if (tower != null)
                tower.dataIndex.Value = index;
        }
    }

    private PlayerController FindPlayerByClientId(ulong clientId)
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
            client.PlayerObject != null)
        {
            var pc = client.PlayerObject.GetComponent<PlayerController>();
            if (pc != null) return pc;
        }

        var all = FindObjectsOfType<PlayerController>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].OwnerClientId == clientId) return all[i];
        }
        return null;
    }

    public void OnTowerBuilt()
    {
    }
}