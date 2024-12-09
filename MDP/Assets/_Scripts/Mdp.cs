using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        [SerializeField] private GameObject _nodeHolder;
        [SerializeField] private Vector2 _gridWorldSize;
        [SerializeField] private float _nodeRadius;
        #endregion
        #endregion

        #region Private Methods

        private void Start()
        {
            StartCoroutine(PolicyIteration());
        }
        #region Value Iteration
        private IEnumerator ValueIteration()
        {
            var grid = new Grid(_nodeObj, _nodeHolder, _gridWorldSize, _nodeRadius);
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

        }
        #endregion

        #region Policy Iteration
        private IEnumerator PolicyIteration()
        {
            var grid = new Grid(_nodeObj, _nodeHolder, _gridWorldSize, _nodeRadius);
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
                        var (newValue, _) = grid.UpdatePolicy(node, node.NodeDirection, _discount, _reward, _noise);

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
                Debug.Log($"{k} iteration end.");
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

        #endregion
    }
}