using System;
using System.Collections.Generic;
using UnityEngine;
using static Assets._Scripts.Node;
using TMPro;

namespace Assets._Scripts
{
    public class Grid : MonoBehaviour
    {
        #region Public Variables
        public Vector2 _gridWorldSize;
        public float _nodeRadius;

        #endregion

        #region Private Variables
        [SerializeField] private GameObject _nodeObj;
        [SerializeField] private GameObject _nodeHolder;
        private Node[,] _grid;
        private float _nodeDiameter;
        private int _gridSizeX, _gridSizeY;

        private Vector2Int[] _neighborsOffset = new Vector2Int[]
        {
            // down - left - up - right
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 0)
        };
        #endregion

        #region Private Methods
        private void Awake()
        {
            _nodeDiameter = _nodeRadius * 2;
            _gridSizeX = Mathf.RoundToInt(_gridWorldSize.x / _nodeDiameter);
            _gridSizeY = Mathf.RoundToInt(_gridWorldSize.y / _nodeDiameter);

        }
        public void GenerateGrid()
        {
            for (var i = 0; i < _nodeHolder.transform.childCount; i++)
            {
                Destroy(_nodeHolder.transform.GetChild(i).gameObject);
            }
            _grid = new Node[_gridSizeX, _gridSizeY];

            var worldBottomLeft = transform.position - Vector3.right * _gridWorldSize.x / 2 - Vector3.forward * _gridWorldSize.y / 2;
            for (var x = 0; x < _gridSizeX; x++)
            {
                for (var y = 0; y < _gridSizeY; y++)
                {
                    var worldPoint = worldBottomLeft + Vector3.right * (x * _nodeDiameter + _nodeRadius) + Vector3.forward * (y * _nodeDiameter + _nodeRadius);
                    var (state, color, value) = DetermineNodeState(x, y);
                    var newNode = _grid[x, y] = new Node(
                        worldPoint, x, y, state, value
                        );
                    newNode.NodeGameObject = InstantiateNodeGameObject(_nodeObj, worldPoint, _nodeHolder, color, value);
                }
            }
        }
        private (NodeStates, Color, float) DetermineNodeState(int x, int y)
        {
            if (x == 1 && y == 1) return (NodeStates.Wall, Color.gray, -2);
            if (x == 3 && y == 1) return (NodeStates.Fire, Color.red, -1);
            if (x == 3 && y == 2) return (NodeStates.Diamond, Color.green, 1);

            return (NodeStates.Empty, Color.white, 0);
        }
        private GameObject InstantiateNodeGameObject(GameObject nodeObject, Vector3 position, GameObject holder, Color color, float value)
        {
            var nodeGameObject = Instantiate(nodeObject, position, Quaternion.identity);
            nodeGameObject.transform.parent = holder.transform;

            var renderer = nodeGameObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            var textComponent = nodeGameObject.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.SetText(Math.Abs(value - (-2)) < 0.001 ? string.Empty : value.ToString());
            }

            return nodeGameObject;
        }


        #endregion

        #region Public Methods
        //public List<Node> GetNeighbors(Node node)
        //{
        //    var neighbors = new List<Node>(4);

        //    foreach (var offset in _neighborsOffset)
        //    {
        //        var checkX = node.gridX + offset.x;
        //        var checkY = node.gridY + offset.y;

        //        if (checkX < 0 || checkX >= _gridSizeX || checkY < 0 || checkY >= _gridSizeY) continue;
        //        var neighborInGrid = _grid[checkX, checkY];
        //        var newNode = new Node(
        //            neighborInGrid.walkable,
        //            neighborInGrid.worldPosition,
        //            checkX,
        //            checkY,
        //            neighborInGrid.nodeColor
        //        );
        //        newNode.nodeGameObject = neighborInGrid.nodeGameObject;
        //        neighbors.Add(newNode);
        //    }
        //    return neighbors;

        //}


        public Node NodeFromWorldPoint(Vector3 worldPosition)
        {
            var percentX = (worldPosition.x + _gridWorldSize.x / 2) / _gridWorldSize.x;
            var percentY = (worldPosition.z + _gridWorldSize.y / 2) / _gridWorldSize.y;

            var x = Mathf.FloorToInt(Mathf.Clamp((_gridSizeX) * percentX, 0, _gridSizeX - 1));
            var y = Mathf.FloorToInt(Mathf.Clamp((_gridSizeY) * percentY, 0, _gridSizeY - 1));

            return _grid[x, y];
        }
        #endregion
    }
}
