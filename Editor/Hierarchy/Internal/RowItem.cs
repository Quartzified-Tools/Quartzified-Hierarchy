using UnityEngine;
using UnityEngine.SceneManagement;

namespace Quartzified.Tools.Hierarchy
{
    internal class RowItem
    {
        public int ID = int.MinValue;
        public Rect rect;
        public Rect nameRect;
        public int rowIndex = 0;
        public GameObject gameObject;
        public bool isNull = true;
        public bool isPrefab = false;
        public bool isPrefabMissing = false;
        public bool isRootObject = false;
        public bool isSelected = false;
        public bool isFirstRow = false;
        public bool isFirstElement = false;
        public bool isDirty = false;
        public bool isMouseHovering = false;

        public string name
        {
            get { return isNull ? "Null" : gameObject.name; }
        }

        public int childCount
        {
            get { return gameObject.transform.childCount; }
        }

        public Scene Scene
        {
            get { return gameObject.scene; }
        }

        public bool isStatic
        {
            get { return isNull ? false : gameObject.isStatic; }
        }

        public RowItem()
        {
        }

        public void Dispose()
        {
            ID = int.MinValue;
            gameObject = null;
            rect = Rect.zero;
            nameRect = Rect.zero;
            rowIndex = 0;
            isNull = true;
            isRootObject = false;
            isSelected = false;
            isFirstRow = false;
            isFirstElement = false;
            isDirty = false;
            isMouseHovering = false;
        }
    }
}

