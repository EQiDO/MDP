using UnityEngine;

namespace Assets._Scripts
{
    public class Node
    {
        #region Public Variables
        public NodeStates NodeState { get;}
        public Vector3 WorldPosition { get; }
        public int GridX { get; }
        public int GridY { get; }
        public Vector2 Position => new(GridX, GridY);
        public GameObject NodeGameObject { get; set; }
        public float NodeValue { get; set; }
        #endregion

        #region Constructor
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

        public bool CheckState(NodeStates state)
        {
            return NodeState == state;
        }

        public void SetValue(float value)
        {
            NodeValue = value;
        }
        #endregion
    }
}
