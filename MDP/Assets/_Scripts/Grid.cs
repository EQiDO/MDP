using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

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
        private bool _show;

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
        public bool Show => _show;

        #endregion

        #region Ctor
        public Grid(GameObject nodeObj, GameObject nodeHolder, Vector2 gridWorldSize, float nodeRadius, bool show)
        {
            _nodeGameObject = nodeObj;
            _nodeHolder = nodeHolder;
            _gridWorldSize = gridWorldSize;
            _nodeRadius = nodeRadius;

            InitializeGrid(show);
        }

        #endregion

        #region Private Methods
        private void InitializeGrid(bool show)
        {
            _nodeDiameter = _nodeRadius * 2;
            _gridSizeX = Mathf.RoundToInt(_gridWorldSize.x / _nodeDiameter);
            _gridSizeY = Mathf.RoundToInt(_gridWorldSize.y / _nodeDiameter);
            _show = show;
            _grid = new Node[_gridSizeX, _gridSizeY];

            var worldBottomLeft = new Vector3(-_gridWorldSize.x / 2, 0, -_gridWorldSize.y / 2);

            for (var x = 0; x < _gridSizeX; x++)
            {
                for (var y = 0; y < _gridSizeY; y++)
                {
                    var worldPoint = worldBottomLeft + Vector3.right * (x * _nodeDiameter + _nodeRadius) + Vector3.forward * (y * _nodeDiameter + _nodeRadius);
                    var node = _grid[x, y] = new Node(
                        worldPoint, x, y
                    );
                    if (show)
                    {
                        var nodeGameObject = InstantiateNodeGameObject(_nodeGameObject, worldPoint, _nodeHolder);
                        node.SetNodeGameObject(nodeGameObject);
                    }
                    node.SetNodeData(NodeStates.Empty, Color.black, 0);
                }
            }
            AssignRandomStates();
        }
        
        private void AssignRandomStates()
        {
            //_grid[3, 2].SetNodeData(NodeStates.Diamond, Color.green * 0.5f, 1);
            //_grid[3, 1].SetNodeData(NodeStates.Fire, Color.red, -1);
            //_grid[1, 1].SetNodeData(NodeStates.Wall, Color.gray, float.NaN);
            //_grid[1, 1].NodeDirectionTransform.gameObject.SetActive(false);

            var assignedPositions = new HashSet<Vector2Int>();

            AssignState(assignedPositions, NodeStates.Diamond, Color.green * 0.5f, 1);
            AssignState(assignedPositions, NodeStates.Fire, Color.red, -1);

            var wallPercent = Random.Range(0.25f, 0.6f);

            var wallCount = (_gridSizeX * _gridSizeY) * wallPercent;
            for (var i = 0; i < wallCount; i++)
            {
                AssignState(assignedPositions, NodeStates.Wall, Color.gray, float.NaN);

            }
        }

        private void AssignState(HashSet<Vector2Int> assignedPositions, NodeStates state, Color color, float value)
        {
            var nodePosition = GetUniqueRandomPosition(assignedPositions);
            var node = _grid[nodePosition.x, nodePosition.y];
            node.SetNodeData(state, color, value);
            if(_show)
                node.NodeDirectionTransform.gameObject.SetActive(false);
        }
        private Vector2Int GetUniqueRandomPosition(HashSet<Vector2Int> assignedPositions)
        {
            Vector2Int position;
            do
            {
                position = new Vector2Int(Random.Range(0, _gridSizeX), Random.Range(0, _gridSizeY));
            } while (assignedPositions.Contains(position));

            assignedPositions.Add(position);
            return position;
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
        public void ResetGrid()
        {
            foreach (var node in GetAllNodes)
            {
                if (!node.CheckState(NodeStates.Empty)) continue;
                node.SetValue(0f);
                node.SetNodeGameObjectText(0f);
                node.SetDirection(Vector2Int.up);
                node.SetNodeGameObjectColor(Color.black);
                if(_show)
                    node.NodeDirectionTransform.localPosition = new Vector3(0, 0.5f, 0.4f);
            }
        }

        public void UpdateGrid()
        {
            if(!_show) return;

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
