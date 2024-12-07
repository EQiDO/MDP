using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets._Scripts
{
    public class Mdp : MonoBehaviour
    {
        private Grid _grid;
        [SerializeField] private float _discount = 0.9f;
        [SerializeField] private float _reward = 0.0f;
        [SerializeField] private float _noise = 0.2f;
        private const double Maxerror = 0.001;
        private void Awake()
        {
            _grid = GetComponent<Grid>();
        }

        private void Start()
        {
            Invoke(nameof(StartMdp), 1);
        }

        private void StartMdp()
        {
            var k = 0;
            while (true)
            {
                var hasChange = false;

                var updatedValues = new Dictionary<Node, float>();

                for (var x = 0; x < _grid.gridSizeX; x++)
                {
                    for (var y = 0; y < _grid.gridSizeY; y++)
                    {
                        var node = _grid.GetNode(x, y);
                        var value = _grid.UpdateValues(node, _discount, _reward, _noise);

                        if (!updatedValues.ContainsKey(node))
                        {
                            updatedValues.Add(node, value);
                            Debug.Log($"initialize {node.Position}, {value}");
                        }
                        else
                        {
                            if (updatedValues.TryGetValue(node, out var oldValue))
                            {
                                if (Math.Abs(oldValue - value) < Maxerror) continue;
                                updatedValues[node] = value;
                                Debug.Log($"update {node.Position}, {value}");
                            }
                        }

                    }
                }

                foreach (var (node, value) in updatedValues)
                {
                    if (Math.Abs(node.NodeValue - value) >= Maxerror)
                    {
                        hasChange = true;
                        node.SetValue(value);
                    }
                }

                if (!hasChange)
                {
                    Debug.Log(k);
                    break;
                }
                k++;
            }
            _grid.UpdateGrid();
        }

        private void LogHashSet(HashSet<Node> nodes)
        {
            foreach (var node in nodes)
            {
                Debug.Log($"Node at position {node.Position} with value {node.NodeValue}");
            }

        }
    }
}