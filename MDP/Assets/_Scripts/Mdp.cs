using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets._Scripts
{
    public class Mdp : MonoBehaviour
    {
        #region Private Variables
        #region Mdp settings
        [SerializeField] private float _discount = 0.9f;
        [SerializeField] private float _reward = 0.0f;
        [SerializeField] private float _noise = 0.2f;
        [SerializeField] private float _delay = 0.5f;
        private const double Maxerror = 1e-4;

        #endregion

        #region Grid Attributes 
        [SerializeField] private GameObject _nodeObj;
        [SerializeField] private Vector2 _gridWorldSize;
        [SerializeField] private float _nodeRadius;
        #endregion
        #endregion

        #region Private Methods
        private void Start()
        {
            //StartCoroutine(ValueIteration());
            StartCoroutine(PolicyIteration());
        }
        #region Value Iteration
        private IEnumerator ValueIteration()
        {
            var nodeHolder = new GameObject("Node Holder");
            var grid = new Grid(_nodeObj, nodeHolder, _gridWorldSize, _nodeRadius);
            var k = 0;

            do
            {
                var hasChange = false;

                var updatedValues = new Dictionary<Node, (float value, Vector2Int direction)>();
                foreach (var node in grid.GetAllNodes)
                {
                    var (value, direction) = grid.UpdateValue(node, _discount, _reward, _noise);
                    updatedValues.Add(node, (value, direction));
                }

                foreach (var (node, (value, direction)) in updatedValues)
                {
                    if (Math.Abs(node.NodeValue - value) >= Maxerror)
                    {
                        hasChange = true;
                        node.SetValue(value);
                        node.SetNodeGameObjectColor(Color.green);
                    }
                    node.SetDirection(direction);
                }

                if (!hasChange)
                {
                    Debug.Log($"Iteration finished, k = {k}");
                    break;
                }
                k++;
                yield return new WaitForSeconds(_delay);
                grid.UpdateGrid();

            } while (true);
            TestRandomRobot(100, grid);
        }
        #endregion

        #region Policy Iteration
        private IEnumerator PolicyIteration()
        {
            var nodeHolder = new GameObject("Node Holder");
            var grid = new Grid(_nodeObj, nodeHolder, _gridWorldSize, _nodeRadius);
            var k = 0;

            while (true)
            {
                bool hasChange;
                do
                {
                    hasChange = false;

                    foreach (var node in grid.GetAllNodes)
                    {
                        var oldValue = node.NodeValue;
                        var newValue = grid.UpdatePolicy(node, node.NodeDirection, _discount, _reward, _noise);

                        if (Math.Abs(oldValue - newValue) >= Maxerror)
                        {
                            hasChange = true;
                            node.SetValue(newValue);
                            node.SetNodeGameObjectColor(Color.green);
                        }
                    }

                    yield return new WaitForSeconds(_delay);
                    grid.UpdateGrid();
                } while (hasChange);

                var policyStable = true;

                foreach (var node in grid.GetAllNodes)
                {
                    var oldPolicy = node.NodeDirection;

                    var (_, bestDirection) = grid.UpdateValue(node, _discount, _reward, _noise);

                    node.SetDirection(bestDirection);

                    if (oldPolicy != bestDirection)
                    {
                        policyStable = false;
                    }
                }

                if (policyStable)
                {
                    Debug.Log($"Policy Iteration converged after {k} iterations.");
                    yield break;
                }

                k++;

                yield return new WaitForSeconds(_delay);
                grid.UpdateGrid();
            }
        }
        #endregion

        #region Test
        private void TestRandomRobot(int numberOfSimulations, Grid valueGrid)
        {
            foreach (var node in valueGrid.GetAllNodes)
            {
                if (node.CheckState(NodeStates.Wall) || node.CheckState(NodeStates.Diamond) || node.CheckState(NodeStates.Fire))
                    continue;

                var totalReward = 0f;
                for (var i = 0; i < numberOfSimulations; i++)
                {
                    var reward = SimulateRandomRobot(node, valueGrid);
                    totalReward += reward;
                }

                var averageReward = totalReward / numberOfSimulations;

                Debug.Log($"Start Node ({node.GridX}, {node.GridY}): Average Reward = {averageReward:F3}");
            }
        }

        private float SimulateRandomRobot(Node startNode, Grid valueGrid)
        {
            var currentNode = startNode;
            var totalReward = 0f;
            var iteration = 0;
            while (true)
            {
                iteration++;
                if (currentNode.CheckState(NodeStates.Diamond) || currentNode.CheckState(NodeStates.Fire))
                   break;

                var policy = currentNode.NodeDirection;

                var randomPercent = Random.Range(1, 101);

                if (randomPercent < (1 - _noise) * 100)
                {
                    var neighborNode = valueGrid.GetValidNeighbor(currentNode, policy.x, policy.y);
                    if(neighborNode == null || neighborNode.CheckState(NodeStates.Wall)) continue;
                    currentNode = neighborNode;
                }
                else
                {
                    var otherDirections = valueGrid.GetOtherDirections(policy);
                    otherDirections.Remove(policy);
                    var randomNeighborIndex = Random.Range(0, otherDirections.Count);
                    var randomNeighbor = otherDirections[randomNeighborIndex];

                    var neighborNode = valueGrid.GetValidNeighbor(currentNode, randomNeighbor.x, randomNeighbor.y);

                    if (neighborNode == null || neighborNode.CheckState(NodeStates.Wall)) continue;
                    currentNode = neighborNode;
                }

                totalReward += _reward;
            }
            return totalReward + currentNode.NodeValue * MathF.Pow(_discount, iteration - 1);
        }

        #endregion

        #endregion
    }
}