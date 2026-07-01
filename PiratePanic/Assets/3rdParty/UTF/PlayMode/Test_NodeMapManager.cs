using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using PiratePanic;
using System.Reflection;
using System;

[TestFixture]
public class Test_NodeMapManager
{
    private GameObject _testRoot; // 统一测试根，所有测试物体作为子物体，一键销毁

    private GameObject mapObj;
    private NodeMapManager mapManager;
    private GameObject battleCtrlObj;
    private Scene02BattleController battleCtrl;
    
    private FieldInfo _nodePrefabField; // 缓存预制体私有字段，避免每次反射查找
    private GameObject _validNodePrefab; // 测试用的Node预制体模板
    
    // 多测试用例的常量
    private const float DefaultSpace = 2f;
    private const float DefaultCutSize = 2f;
    private Vector2Int DefaultMapSize = new Vector2Int(6, 6);

    [SetUp]
    public void CaseSetUp()
    {
        // 获取当前测试上下文
        var context = TestContext.CurrentContext;
        Debug.Log($"完整路径：{context.Test.Name}"); // 测试方法全名：命名空间.类名.方法名
        Debug.Log($"当前执行测试方法：{context.Test.FullName}"); // 仅测试方法名（只取函数名）

        // 0. 创建统一测试根物体，所有测试对象挂在下面
        _testRoot = new GameObject("TestRoot");

        // 1. 创建独立战斗控制器（不使用全局单例）
        battleCtrlObj = CreateChildObj("TestBattleCtrl");
        battleCtrl = battleCtrlObj.AddComponent<Scene02BattleController>();
        battleCtrl.Test_ResetMapRecord();

        // 2. 创建Node预制体（临时，给NodeMapManager._nodePrefab用）
        GameObject tempNodePrefab = new GameObject("TestNodePrefab");
        tempNodePrefab.AddComponent<Node>(); // 挂载Node组件
        _validNodePrefab = UnityEngine.Object.Instantiate(tempNodePrefab);
        UnityEngine.Object.DestroyImmediate(tempNodePrefab); // 销毁模板

        // 3. 创建地图管理器，默认关闭
        mapObj = CreateChildObj("NodeMap");
        mapObj.SetActive(false);
        // 添加组件，此时物体未激活
        mapManager = mapObj.AddComponent<NodeMapManager>();
        mapManager.InjectBattleController(battleCtrl); // 注入依赖
        // 缓存反射字段，全局复用
        _nodePrefabField = typeof(NodeMapManager)
            .GetField("_nodePrefab", BindingFlags.Instance | BindingFlags.NonPublic);

    }


    [TearDown]
    public void CaseTearDown()
    {
        var context = TestContext.CurrentContext;
        Debug.Log($"执行完毕，清理<{context.Test.Name}>环境");
        // 只销毁根物体，自动回收所有子物体，无需逐个Destroy
        UnityEngine.Object.DestroyImmediate(_testRoot);
        _validNodePrefab = null;

        // 销毁用例生成的场景内所有Node实例
        var allNodes = UnityEngine.Object.FindObjectsOfType<Node>();
        foreach (var n in allNodes) UnityEngine.Object.Destroy(n.gameObject);
    }

    
    #region 异常场景
    [Test]
    public void Test_Exception_EmptyNodePrefab()
    {
        // 场景：Node预制体为空时,实例化节点应不抛出异常
        SetNodePrefab(null); // 置空预制体
        InitMapConfig(DefaultMapSize, DefaultSpace, DefaultCutSize);

        Assert.IsNotNull(mapManager.MapNodes);
        Assert.AreEqual(0,GetNonEmptyNodeCount(mapManager.MapNodes));

    }

    [Test]
    public void Test_Exception_ZeroSizeMap()
    {
        // 场景：地图尺寸为0时，应不崩溃且节点数组为空
        mapManager.Size = Vector2Int.zero;
        SetNodePrefab(_validNodePrefab);
        InitMapConfig(Vector2Int.zero, DefaultSpace, DefaultCutSize);

        Assert.IsNotNull(mapManager.MapNodes);
        Assert.Throws<IndexOutOfRangeException>(() => { var _ = mapManager.MapNodes[0, 0]; });
    }

    [Test]
    public void Test_Exception_NegativeSpaceDistance()
    {
        // 场景：间距为负数时，坐标计算应不崩溃
        SetNodePrefab(_validNodePrefab);
        InitMapConfig(DefaultMapSize, -2f, DefaultCutSize);

        // 验证：节点仍能生成，坐标为负数但无异常
        int nodeCount = GetNonEmptyNodeCount(mapManager.MapNodes);
        Assert.Greater(nodeCount, 0);
    }

    #endregion
    
    #region 极限边界测试
    [Test]
    public void Test_Boundary_MinMapSize()
    {
        // 场景：最小尺寸（1x1）地图，验证节点生成逻辑
        SetNodePrefab(_validNodePrefab);
        InitMapConfig(new Vector2Int(1, 1), DefaultSpace, 0f);

        Node node = mapManager.MapNodes[0, 0];
        Assert.IsNotNull(node);
        Assert.AreEqual(new Vector3(0,0,0),node.transform.position);
    }

    [Test]
    public void Test_Boundary_OneRowMap()
    {
        // 场景：单行地图（6x1），验证节点排列和连接
        SetNodePrefab(_validNodePrefab);
        InitMapConfig(new Vector2Int(6, 1), DefaultSpace, 0f);

        // 验证：所有节点生成正确，双向连接正确
        int nodeCount = GetNonEmptyNodeCount(mapManager.MapNodes);
        Assert.AreEqual(6, nodeCount);

        for(int x = 1; x < 6; x++)
        {
            Node current = mapManager.MapNodes[x, 0];
            Node pre = mapManager.MapNodes[x - 1, 0];
            Assert.IsTrue(current.ConnectedNodes.ContainsKey(pre));
            Assert.IsTrue(pre.ConnectedNodes.ContainsKey(current));
        }
    }
    #endregion

    #region 六边形坐标计算核心逻辑
    [Test]
    public void Test_HexCoordinate_EvenRow()
    {
        // 场景：偶数行（y%2==0）坐标计算验证
        // 公式：(x,y,z)=(x * SpaceDistance, 0, y * ((SpaceDistance*0.5f/Sin(π/3)) + 0.25f*SpaceDistance))
        SetNodePrefab(_validNodePrefab);
        InitMapConfig(DefaultMapSize, DefaultSpace, DefaultCutSize);
        
        float space =2f;
        float zStep=(space * 0.5f / Mathf.Sin(Mathf.PI / 3f)) + 0.25f * space;

        // 验证y=0（偶数行）x=3的坐标
        Vector3 expected = new Vector3(3 * space, 0, 0 * zStep);
        Node node = mapManager.MapNodes[3, 0];
        Assert.IsNotNull(node);
        Assert.AreEqual(expected.x,node.transform.position.x,0.01f);
        Assert.AreEqual(expected.z,node.transform.position.z,0.01f);
    }

    [Test]
    public void Test_HexCoordinate_OddRow()
    {
        // 场景：奇数行（y%2!=0）坐标计算验证
        // 公式：x*SpaceDistance + 0.5*SpaceDistance, 0, y*zStep
        SetNodePrefab(_validNodePrefab);
        InitMapConfig(DefaultMapSize, DefaultSpace, DefaultCutSize);

        float space = 2f;
        float zStep = (space * 0.5f / Mathf.Sin(Mathf.PI / 3f)) + 0.25f * space;

        // 验证y=1（奇数行）x=3的坐标
        Vector3 expected = new Vector3(0.5f*space+3*space,0,1*zStep);
        Node node= mapManager.MapNodes[3,1];
        Assert.IsNotNull(node);
        Assert.AreEqual(expected.x, node.transform.position.x, 0.01f);
        Assert.AreEqual(expected.z, node.transform.position.z, 0.01f);

    }

    [Test]
    public void Test_HexCoordinate_CenterCut()
    {
        // 场景：中心裁剪区域内的节点不生成
        SetNodePrefab(_validNodePrefab);
        InitMapConfig(DefaultMapSize, 2f, 1f);

        Vector2 center = new Vector2((6 - 1) * 2f, (6 - 1) * ((2f * 0.5f / Mathf.Sin(Mathf.PI / 3f)) + 0.25f * 2f)) / 2f;
        Node centerNode = mapManager.MapNodes[2, 3];
        Assert.IsNull(centerNode);

    }

    #endregion

    #region 节点实例化 & 双向邻接连接测试
    [Test]
    public void Test_NodeInstantiate_Count()
    {
        // 验证：非中心裁剪区域的节点正确生成
        SetNodePrefab(_validNodePrefab);
        InitMapConfig(DefaultMapSize, DefaultSpace, DefaultCutSize);

        int nodeCount = GetNonEmptyNodeCount(mapManager.MapNodes);
        // 6x6地图，中心裁剪2f，预期生成约25个节点（可根据实际逻辑调整）
        Assert.Greater(nodeCount, 20);
        Assert.Less(nodeCount, 36);
    }

    [Test]
    public void Test_NodeConnect_EvenRow()
    {
        // 场景：偶数行节点（y=0）的邻接连接（左、左上、左下）
        SetNodePrefab(_validNodePrefab);
        InitMapConfig(DefaultMapSize, DefaultSpace, DefaultCutSize);

        Node node = mapManager.MapNodes[2, 0];
        Assert.IsNotNull(node);

        // 验证：左节点（1,0）左上（1，1）、左下（1，-1）双向连接
        Node leftNode=mapManager.MapNodes[1, 0];
        if (leftNode != null)
        {
            Assert.IsTrue(node.ConnectedNodes.ContainsKey(leftNode));
            Assert.IsTrue(leftNode.ConnectedNodes.ContainsKey(node));
        }
        Node leftUpNode=mapManager.MapNodes[1, 1];
        if (leftUpNode != null)
        {
            Assert.IsTrue(node.ConnectedNodes.ContainsKey(leftUpNode));
            Assert.IsTrue(leftUpNode.ConnectedNodes.ContainsKey(node));
        }
        // Node leftDownNode=mapManager.MapNodes[1, -1];
        // if (leftUpNode != null)
        // {
        //     Assert.IsTrue(node.ConnectedNodes.ContainsKey(leftDownNode));
        //     Assert.IsTrue(leftDownNode.ConnectedNodes.ContainsKey(node));
        // }
    }

    [Test]
    public void Test_NodeConnection_OddRow()
    {
        // 场景：奇数行节点（y=1）的邻接连接
        SetNodePrefab(_validNodePrefab);
        InitMapConfig(DefaultMapSize, DefaultSpace, DefaultCutSize);

        Node node = mapManager.MapNodes[2, 1];
        Assert.IsNotNull(node);

        // 验证：上节点（2,0）双向连接
        Node upNode = mapManager.MapNodes[2, 0];
        if (upNode != null)
        {
            Assert.IsTrue(node.ConnectedNodes.ContainsKey(upNode));
            Assert.IsTrue(upNode.ConnectedNodes.ContainsKey(node));
        }

        // 验证：左节点（1,1）双向连接
        Node leftNode = mapManager.MapNodes[1, 1];
        if (leftNode != null)
        {
            Assert.IsTrue(node.ConnectedNodes.ContainsKey(leftNode));
            Assert.IsTrue(leftNode.ConnectedNodes.ContainsKey(node));
        }
    }

    [Test]
    public void Test_NodeConnection_Bidirectional()
    {
        // 场景：任意两个连接的节点必须双向引用
        SetNodePrefab(_validNodePrefab);
        InitMapConfig(DefaultMapSize, DefaultSpace, DefaultCutSize);
        
        Node[,] nodes = mapManager.MapNodes;
        for(int x = 0; x < nodes.GetLength(0); x++)
        {
            for(int y = 0; y < nodes.GetLength(1); y++)
            {
                Node current = nodes[x, y];
                if (current == null) continue;

                // 遍历当前节点的所有邻接节点，验证双向连接
                foreach (var kvp in current.ConnectedNodes)
                {
                    Node neighbour = kvp.Key;
                    Assert.IsTrue(neighbour.ConnectedNodes.ContainsKey(current), 
                        $"节点({x},{y})与({neighbour.Position})未双向连接"); //断言失败时输出的自定义错误提示字符串
                }
            }
        }
    }

    #endregion

    #region 辅助方法
    /// <summary>
    /// 在测试根下创建子物体，统一管理生命周期
    /// </summary>
    private GameObject CreateChildObj(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(_testRoot.transform);
        return go;
    }

    /// <summary>
    /// 设置Node预制体，支持传入null
    /// </summary>
    private void SetNodePrefab(GameObject prefab)
    {
        _nodePrefabField.SetValue(mapManager, prefab);
    }

    /// <summary>
    /// 配置地图参数并重新生成地图,尽量需要测的变量都放进测试初始化函数中
    /// </summary>
    private void InitMapConfig(Vector2Int size, float space, float cutSize)
    {
        // // 清理历史节点
        // var oldNodes = UnityEngine.Object.FindObjectsOfType<Node>();
        // foreach (var n in oldNodes)
        //     UnityEngine.Object.DestroyImmediate(n.gameObject);

        mapManager.Size = size;
        mapManager.SpaceDistance = space;
        mapManager.CenterCutSize = cutSize;

        // 重新激活触发Awake
        mapObj.SetActive(false);
        mapObj.SetActive(true);
    }

    /// <summary>
    /// 统计非空节点数量
    /// </summary>
    private int GetNonEmptyNodeCount(Node[,] nodes)
    {
        int count = 0;
        for (int x = 0; x < nodes.GetLength(0); x++)
        {
            for (int y = 0; y < nodes.GetLength(1); y++)
            {
                if (nodes[x, y] != null) count++;
            }
        }
        return count;
    }
    #endregion
}
