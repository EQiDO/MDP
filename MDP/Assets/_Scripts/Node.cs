using TMPro;
using UnityEngine;

namespace Assets._Scripts
{
    public class Node
    {
        #region Properties

        #region Immutable
        public Vector3 WorldPosition { get; }
        public int GridX { get; }
        public int GridY { get; }
        public Vector2Int Position => new(GridX, GridY);
        #endregion

        #region Mutable
        public GameObject NodeGameObject { get; private set; }
        public NodeStates NodeState { get; private set; }
        public Transform NodeDirectionTransform => NodeGameObject?.gameObject.transform.GetChild(1);
        public Material NodeGameObjectMaterial => NodeGameObject?.GetComponent<MeshRenderer>().material;
        public TMP_Text NodeGameObjectText => NodeGameObject?.GetComponentInChildren<TMP_Text>();
        public float NodeValue { get; private set; }
        public Vector2Int NodeDirection { get; private set; } = Vector2Int.up;
        #endregion

        #endregion

        #region Ctor
        public Node(Vector3 worldPosition, int gridX, int gridY)
        {
            WorldPosition = worldPosition;
            GridX = gridX;
            GridY = gridY;
        }
        #endregion

        #region Public Methods

        public bool CheckState(NodeStates state) => NodeState == state;

        public void SetNodeGameObject(GameObject nodeGameObject)
        {
            NodeGameObject = nodeGameObject;
        }
        public void SetValue(float value)
        {
            NodeValue = value;
        }

        public void SetNodeData(NodeStates state, Color color, float value)
        {
            SetNodeState(state);
            SetValue(value);
            SetNodeGameObjectText(value);
            SetNodeGameObjectColor(color);
        }
        public void SetDirection(Vector2Int direction)
        {
            NodeDirection = direction;
        }

        public void SetNodeState(NodeStates state)
        {
            NodeState = state;
        }
        public void SetNodeGameObjectColor(Color color)
        {
            if (NodeGameObject == null) return;
            NodeGameObjectMaterial.color = color;
        }

        public void SetNodeGameObjectText(float value)
        {
            if (NodeGameObject == null) return;
            NodeGameObjectText?.SetText(float.IsNaN(value) ? null : value.ToString("F2"));
        }
        #endregion
    }
}
