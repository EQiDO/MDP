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
            StartCoroutine(StartMdp());
        }

        private IEnumerator StartMdp()
        {
            var grid = new Grid(_nodeObj, _nodeHolder, _gridWorldSize, _nodeRadius);
            var (gridSizeX, gridSizeY) = grid.GridSize;

            var k = 0;
            while (true)
            {
                var hasChange = false;

                var updatedValues = new Dictionary<Node, (float value, Vector2Int direction)>();

                for (var x = 0; x < gridSizeX; x++)
                {
                    for (var y = 0; y < gridSizeY; y++)
                    {
                        var node = grid.GetNode(x, y);
                        var (value, direction) = grid.UpdateValue(node, _discount, _reward, _noise);

                        updatedValues.Add(node, (value, direction));
                        //Debug.Log($"initialize {node.Position}, {value}");
                    }
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
                    yield break;
                }
                k++;
                yield return new WaitForSeconds(_delay);
                grid.UpdateGrid();
            }
        }
        #endregion

    }
}