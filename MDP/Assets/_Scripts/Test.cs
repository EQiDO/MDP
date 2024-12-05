using UnityEngine;

namespace Assets._Scripts
{
    public class Test : MonoBehaviour
    {
        private Grid _grid;

        private void Awake()
        {
        }
        private void Start()
        {
            _grid = GetComponent<Grid>();

            _grid.GenerateGrid();
        }
    }
}
