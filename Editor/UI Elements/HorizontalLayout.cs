using UnityEngine.UIElements;

namespace Quartzified.Tools.Hierarchy
{
    public class HorizontalLayout : VisualElement
    {
        public HorizontalLayout()
        {
            name = nameof(HorizontalLayout);
            this.StyleFlexDirection(FlexDirection.Row);
            this.StyleFlexGrow(1);
        }
    }
}