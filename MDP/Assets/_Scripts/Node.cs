using TMPro;
using UnityEngine;

namespace Assets._Scripts
{
    public class Node
    {
        #region Properties

        #region Immutable
        public NodeStates NodeState { get; }
        public Vector3 WorldPosition { get; }
        public int GridX { get; }
        public int GridY { get; }
        public Vector2 Position => new(GridX, GridY);
        #endregion

        #region Mutable
        public GameObject NodeGameObject { get; private set; }
        public Transform NodeDirectionTransform => NodeGameObject.gameObject.transform.GetChild(1);
        public Material NodeGameObjectMaterial => NodeGameObject.GetComponent<MeshRenderer>().material;
        public TMP_Text NodeGameObjectText => NodeGameObject.GetComponentInChildren<TMP_Text>();
        public float NodeValue { get; private set; }
        public Vector2Int NodeDirection { get; private set; }
        #endregion

        #endregion

        #region Ctor
        public Node(Vector3 worldPosition, int gridX, int gridY, NodeStates nodeState, float nodeValue)
        {
            WorldPosition = worldPosition;
            GridX = gridX;
            GridY = gridY;
            NodeState = nodeState;
            NodeValue = nodeValue;
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

        public void SetDirection(Vector2Int direction)
        {
            NodeDirection = direction;
        }

        public void SetNodeGameObjectColor(Color color)
        {
            NodeGameObjectMaterial.color = color;
        }

        public void SetNodeGameObjectText(float value)
        {
            NodeGameObjectText.SetText(float.IsNaN(value) ? null : value.ToString("F2"));
        }
        #endregion
    }
}
