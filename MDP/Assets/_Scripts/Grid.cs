using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Assets._Scripts.Node;
using TMPro;

namespace Assets._Scripts
{
    public class Grid : MonoBehaviour
    {
        #region Private Variables
        [SerializeField] private GameObject _nodeObj;
        [SerializeField] private GameObject _nodeHolder;
        [SerializeField] private Vector2 _gridWorldSize;
        [SerializeField] private float _nodeRadius;

        private Node[,] _grid;
        private float _nodeDiameter;
        public int gridSizeX, gridSizeY;

        public Node startNode;
        private readonly Vector2Int[] _neighborsOffset = new Vector2Int[]
        {
            // right - left - up - down
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
        };
        #endregion

        #region Private Methods
        private void Awake()
        {
            _nodeDiameter = _nodeRadius * 2;
            gridSizeX = Mathf.RoundToInt(_gridWorldSize.x / _nodeDiameter);
            gridSizeY = Mathf.RoundToInt(_gridWorldSize.y / _nodeDiameter);

        }
        private void Start()
        {
            _grid = new Node[gridSizeX, gridSizeY];

            var worldBottomLeft = transform.position - Vector3.right * _gridWorldSize.x / 2 - Vector3.forward * _gridWorldSize.y / 2;
            for (var x = 0; x < gridSizeX; x++)
            {
                for (var y = 0; y < gridSizeY; y++)
                {
                    var worldPoint = worldBottomLeft + Vector3.right * (x * _nodeDiameter + _nodeRadius) + Vector3.forward * (y * _nodeDiameter + _nodeRadius);
                    var (state, color, value) = DetermineNodeState(x, y);
                    var newNode = _grid[x, y] = new Node(
                        worldPoint, x, y, state, value
                        );
                    newNode.NodeGameObject = InstantiateNodeGameObject(_nodeObj, worldPoint, _nodeHolder, color, value);
                    if (newNode.CheckState(NodeStates.Diamond))
                        startNode = newNode;
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
            renderer.material.color = color;

            var textComponent = nodeGameObject.GetComponentInChildren<TMP_Text>();

            textComponent.SetText(Math.Abs(value - (-2)) < 0.001f ? null : value.ToString());

            return nodeGameObject;
        }


        #endregion

        #region Public Methods
        public List<Node> GetNeighbors(Node node)
        {
            var neighbors = new List<Node>(4);

            foreach (var offset in _neighborsOffset)
            {
                var checkX = node.GridX + offset.x;
                var checkY = node.GridY + offset.y;

                var outOfBoundary = checkX < 0 || checkX >= gridSizeX || checkY < 0 || checkY >= gridSizeY;

                if(outOfBoundary) continue;

                var neighbor = _grid[checkX, checkY];
                var isEmptyState = neighbor.CheckState(NodeStates.Empty);

                if (!isEmptyState) continue;

                neighbors.Add(neighbor);
            }
            return neighbors;

        }
        public Node GetNode(int x, int y)
        {
            if (x < 0 || x >= gridSizeX || y < 0 || y >= gridSizeY) return null;
            return _grid[x, y];
        }
        public void UpdateGrid()
        {
            for (var x = 0; x < gridSizeX; x++)
            {
                for (var y = 0; y < gridSizeY; y++)
                {
                    var node = _grid[x, y];

                    if (node.CheckState(NodeStates.Wall)) continue;

                    var textComponent = node.NodeGameObject.GetComponentInChildren<TMP_Text>();
                    if (textComponent != null)
                    {
                        textComponent.SetText(node.NodeValue.ToString("F2"));
                    }
                }
            }
        }


        public float UpdateValues(Node node, float discount, float reward, float noise)
        {
            var nodeValues = new List<float>();
            
            foreach (var offset in _neighborsOffset)
            {
                if (node.CheckState(NodeStates.Diamond))
                {
                    return 1f;
                }
                if (node.CheckState(NodeStates.Fire))
                {
                    return -1f;
                }

                var checkX = node.GridX + offset.x;
                var checkY = node.GridY + offset.y;

                if (checkX < 0 || checkX >= gridSizeX || checkY < 0 || checkY >= gridSizeY)
                {
                    continue;
                }

                //Debug.Log($"We are checking {checkY}, {checkY}");

                var adjustedOffsets = new List<Vector2Int>(_neighborsOffset);

                adjustedOffsets.Remove(new Vector2Int(-offset.x, -offset.y));

                var primaryPercent = 1 - noise;
                var secondaryPercent = noise / 2;

                float sum = 0;
                foreach (var neighborOffset in adjustedOffsets)
                {

                    var neighborX = node.GridX + neighborOffset.x;
                    var neighborY = node.GridY + neighborOffset.y;

                    //Debug.Log($"Now we are checking {neighborX}, {neighborY} of {checkY}, {checkY}");

                    if (neighborX < 0 || neighborX >= gridSizeX || neighborY < 0 || neighborY >= gridSizeY)
                    { 
                        //Debug.Log($"out of boundary {node.Position}");
                       sum+= CalculateV(secondaryPercent, reward, discount, node);
                       continue;
                    }

                    var adjacentNeighbor = _grid[neighborX, neighborY];
                    if (adjacentNeighbor.CheckState(NodeStates.Wall))
                    {
                        sum += CalculateV(secondaryPercent, reward, discount, node);
                        continue;
                    }

                    var result = CalculateV(neighborOffset == offset ? primaryPercent : secondaryPercent, reward, discount, adjacentNeighbor);
                    sum += result;
                }
                //Debug.Log(sum);
                nodeValues.Add(sum);
            }
            return nodeValues.Max();
        }

        private float CalculateV(float noise, float reward, float discount, Node node)
        {
            //Debug.Log($"{percent} * ({reward} + ({discount} * {node.NodeValue}))");
            return noise * (reward + (discount * node.NodeValue));
        }

        #endregion
    }
}
