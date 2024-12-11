using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using TMPro;
namespace Assets._Scripts
{
    public class Grid
    {
        #region Private Variables
        private readonly GameObject _nodeGameObject;
        private readonly GameObject _nodeHolder;
        private readonly Vector2 _gridWorldSize;
        private readonly float _nodeRadius;

        private Node[,] _grid;
        private float _nodeDiameter;
        private int _gridSizeX, _gridSizeY;

        private readonly Vector2Int[] _neighborsOffset = {
            // up - right - down - left
            new (0, 1),
            new (1, 0),
            new (0, -1),
            new (-1, 0),
        };

        #endregion

        #region Properties
        public IEnumerable<Node> GetAllNodes => _grid.Cast<Node>();
        public Node GetNodeFromGrid(int gridX, int gridY) => _grid[gridX, gridY];

        #endregion

        #region Ctor
        public Grid(GameObject nodeObj, GameObject nodeHolder, Vector2 gridWorldSize, float nodeRadius)
        {
            _nodeGameObject = nodeObj;
            _nodeHolder = nodeHolder;
            _gridWorldSize = gridWorldSize;
            _nodeRadius = nodeRadius;

            InitializeGrid();
        }
        #endregion

        #region Private Methods
        private void InitializeGrid()
        {
            _nodeDiameter = _nodeRadius * 2;
            _gridSizeX = Mathf.RoundToInt(_gridWorldSize.x / _nodeDiameter);
            _gridSizeY = Mathf.RoundToInt(_gridWorldSize.y / _nodeDiameter);

            _grid = new Node[_gridSizeX, _gridSizeY];

            var worldBottomLeft = new Vector3(-_gridWorldSize.x / 2, 0, -_gridWorldSize.y / 2);

            for (var x = 0; x < _gridSizeX; x++)
            {
                for (var y = 0; y < _gridSizeY; y++)
                {
                    var worldPoint = worldBottomLeft + Vector3.right * (x * _nodeDiameter + _nodeRadius) + Vector3.forward * (y * _nodeDiameter + _nodeRadius);
                    var (state, color, value) = DetermineNodeState(x, y);
                    var node = _grid[x, y] = new Node(
                        worldPoint, x, y, state, value
                    );

                    var nodeGameObject = InstantiateNodeGameObject(_nodeGameObject, worldPoint, _nodeHolder);

                    node.SetNodeGameObject(nodeGameObject);
                    node.SetNodeGameObjectColor(color);
                    node.SetNodeGameObjectText(value);
                    if (!node.CheckState(NodeStates.Empty))
                        node.NodeDirectionTransform.gameObject.SetActive(false);
                }
            }
        }
        private (NodeStates, Color, float) DetermineNodeState(int x, int y)
        {
            if (x == 1 && y == 1) return (NodeStates.Wall, Color.gray, float.NaN);
            if (x == 3 && y == 1) return (NodeStates.Fire, Color.red, -1);
            if (x == 3 && y == 2) return (NodeStates.Diamond, Color.green, 1);

            return (NodeStates.Empty, Color.black, 0);
        }
        private GameObject InstantiateNodeGameObject(GameObject nodeObject, Vector3 position, GameObject holder)
        {
            var nodeGameObject = UnityEngine.Object.Instantiate(nodeObject, position, Quaternion.identity);

            nodeGameObject.transform.parent = holder.transform;

            return nodeGameObject;
        }
        private float CalculateV(float noise, float reward, float discount, Node node)
        {
            return noise * (reward + (discount * node.NodeValue));
        }
        #endregion

        #region Public Methods
        public void UpdateGrid()
        {
            for (var x = 0; x < _gridSizeX; x++)
            {
                for (var y = 0; y < _gridSizeY; y++)
                {
                    var node = _grid[x, y];

                    if (!node.CheckState(NodeStates.Empty)) continue;

                    node.SetNodeGameObjectText(node.NodeValue);

                    var childTransform = node.NodeDirectionTransform; 

                    var xVal = node.NodeDirection.x * 0.4f;
                    var zVal = node.NodeDirection.y * 0.4f;
                    childTransform.localPosition = new Vector3(xVal, childTransform.localPosition.y, zVal);
                }
            }
        }
        
        public (float, Vector2Int) UpdateValue(Node node, float discount, float reward, float noise)
        {
            var nodeValues = new List<(float value, Vector2Int direction)>();

            foreach (var offset in _neighborsOffset)
            {
                if (node.CheckState(NodeStates.Diamond) || node.CheckState(NodeStates.Fire))
                {
                    return (node.CheckState(NodeStates.Diamond) ? 1f : -1f, Vector2Int.zero);
                }

                var adjustedOffsets = GetOtherDirections(offset);

                var primaryPercent = 1 - noise;
                var secondaryPercent = noise / 2;

                float sum = 0;
                foreach (var neighborOffset in adjustedOffsets)
                {
                    var adjacentNeighbor = GetValidNeighbor(node, neighborOffset.x, neighborOffset.y);

                    if (adjacentNeighbor == null || adjacentNeighbor.CheckState(NodeStates.Wall))
                    {
                        sum += CalculateV(secondaryPercent, reward, discount, node);
                        continue;
                    }
                     
                    var result = CalculateV(neighborOffset == offset ? primaryPercent : secondaryPercent, reward, discount, adjacentNeighbor);
                    sum += result;
                }
                nodeValues.Add((sum, offset));
            }
            var maxNodeValue = nodeValues.OrderByDescending(nv => nv.value).First();
            return maxNodeValue;
        }

        public float UpdatePolicy(Node node,Vector2Int policy ,float discount, float reward, float noise)
        {
            var adjustedOffsets = GetOtherDirections(policy);

            var primaryPercent = 1 - noise;
            var secondaryPercent = noise / 2;

            float sum = 0;

            foreach (var neighborOffset in adjustedOffsets)
            {
                if (node.CheckState(NodeStates.Diamond) || node.CheckState(NodeStates.Fire))
                {
                    return node.CheckState(NodeStates.Diamond) ? 1f : -1f;
                }

                var adjacentNeighbor = GetValidNeighbor(node, neighborOffset.x, neighborOffset.y);

                if (adjacentNeighbor == null || adjacentNeighbor.CheckState(NodeStates.Wall))
                {
                    sum += CalculateV(secondaryPercent, reward, discount, node);
                    continue;
                }

                var result = CalculateV(policy == neighborOffset ? primaryPercent : secondaryPercent, reward, discount, adjacentNeighbor);
                sum += result;
            }
            return sum;
        }
        public Node GetValidNeighbor(Node node, int x, int y)
        {
            var neighborPosition = node.Position + new Vector2Int(x, y);
            var neighborX = neighborPosition.x;
            var neighborY = neighborPosition.y;
            if (neighborX < 0 || neighborX >= _gridSizeX || neighborY < 0 || neighborY >= _gridSizeY) return null;

            return _grid[neighborX, neighborY];
        }
        public List<Vector2Int> GetOtherDirections(Vector2Int primaryDirection)
        {
            var directions = _neighborsOffset.ToList();
            var opposite = -primaryDirection;
            directions.Remove(opposite);
            return directions;
        }
        #endregion
    }
}
