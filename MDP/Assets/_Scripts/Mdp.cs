using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Threading.Tasks;

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
        private const double MaxError = 1e-4;

        #endregion

        #region Grid Attributes 
        [SerializeField] private GameObject _nodeObj;
        //[SerializeField] private Vector2 _gridWorldSize;
        [SerializeField] private float _gridSizeX = 5;
        [SerializeField] private float _gridSizeY = 20;
        [SerializeField] private float _nodeRadius;

        #endregion

        #endregion

        #region Private Methods

        private void Start()
        {
            TestRandomIterations(10, false, 0);
            //TestRandomIterations(1, true, _delay);

            //StartPolicyIteration();
            //StartValueIteration();
        }
        #region Value Iteration
        private IEnumerator ValueIteration(Grid grid, bool show, float delay)
        {
            var k = 0;

            while (true)
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
                    if (Math.Abs(node.NodeValue - value) >= MaxError)
                    {
                        hasChange = true;
                        node.SetValue(value);
                        if(show)
                            node.SetNodeGameObjectColor(Color.green);
                    }
                    node.SetDirection(direction);
                }

                if (!hasChange)
                {
                    //Debug.Log($"Iteration finished, k = {k}");
                    grid.UpdateGrid();
                    //TestRandomRobot(100, grid);
                    break;
                }

                k++;
                yield return new WaitForSeconds(delay);
            }

        }
        #endregion

        #region Policy Iteration
        private IEnumerator PolicyIteration(Grid grid,bool show, float delay)
        {
            var k = 0;

            while (true)
            {

                // Policy Evaluation
                var hasChange = true;
                while (hasChange)
                {
                    hasChange = false;

                    foreach (var node in grid.GetAllNodes) // until no changes for every nodes with this policy
                    {
                        grid.UpdateGrid();

                        var oldValue = node.NodeValue;
                        var newValue = grid.UpdatePolicy(node, node.NodeDirection, _discount, _reward, _noise);

                        if (Math.Abs(oldValue - newValue) >= MaxError)
                        {
                            hasChange = true;
                            node.SetValue(newValue);
                            if(show)
                                node.SetNodeGameObjectColor(Color.green);
                        }
                    }
                }


                var policyStable = true;
                // Policy Improvement
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
                    //Debug.Log($"Policy Iteration converged after {k} iterations.");
                    grid.UpdateGrid();
                    //TestRandomRobot(100, grid);
                    yield break;
                }

                k++;

                yield return new WaitForSeconds(delay);

            }
        }
        #endregion

        #region Robot
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

        private void StartValueIteration()
        {
            var gridSize = new Vector2(_gridSizeX, _gridSizeY);
            var nodeHolder = new GameObject("Node Holder");
            var grid = new Grid(_nodeObj, nodeHolder, gridSize, _nodeRadius, true);
            StartCoroutine(ValueIteration(grid, true, _delay));
        }
        private void StartPolicyIteration()
        {
            var gridSize = new Vector2(_gridSizeX, _gridSizeY);
            var nodeHolder = new GameObject("Node Holder");
            var grid = new Grid(_nodeObj, nodeHolder, gridSize, _nodeRadius, true);
            StartCoroutine(PolicyIteration(grid, true, _delay));
        }
        private float SimulateRandomRobot(Node startNode, Grid valueGrid)
        {
            var currentNode = startNode;
            var totalReward = 0f;
            var iteration = 0;
            const int maxIterations = 1000;
            while (true)
            {
                iteration++;
                if (currentNode.CheckState(NodeStates.Diamond) || currentNode.CheckState(NodeStates.Fire) || iteration>= maxIterations)
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

        #region Test

        private async void TestRandomIterations(int iterations, bool show, float delay)
        {

            double valueIterationTime = 0;
            double policyIterationTime = 0;

            for (var i = 0; i < iterations; i++)
            {

                var gridSize = new Vector2(_gridSizeX, _gridSizeY);
                var grid = new Grid(_nodeObj, null, gridSize, _nodeRadius, show);

                valueIterationTime += await MeasureExecutionTimeAsync(() => ValueIteration(grid, show, delay));
                grid.ResetGrid();

                policyIterationTime += await MeasureExecutionTimeAsync(() => PolicyIteration(grid, show, delay));
            }

            var valueIterationAvgTime = valueIterationTime / iterations;
            var policyIterationAvgTime = policyIterationTime / iterations;

            print($"Value iteration avg time: {valueIterationAvgTime:F3} seconds.");
            print($"Policy iteration avg time: {policyIterationAvgTime:F3} seconds.");
            print($"Repetitions Count: {iterations}.");
            print($"Grid size is {_gridSizeX} x {_gridSizeY}");
        }

        private async Task<double> MeasureExecutionTimeAsync(Func<IEnumerator> coroutine)
        {
            var sw = new Stopwatch();
            sw.Start();

            var completionSource = new TaskCompletionSource<bool>();
            StartCoroutine(ExecuteCoroutine(coroutine, completionSource));
            await completionSource.Task;

            sw.Stop();
            return sw.Elapsed.TotalSeconds;
        }

        private IEnumerator ExecuteCoroutine(Func<IEnumerator> coroutine, TaskCompletionSource<bool> completionSource)
        {
            yield return StartCoroutine(coroutine());
            completionSource.SetResult(true);
        }

        #endregion
        #endregion
    }
}