using UnityEngine.UIElements;

namespace Quartzified.Tools.Hierarchy
{
    public class VerticalLayout : VisualElement
    {
        public VerticalLayout()
        {
            name = nameof(VerticalLayout);
            this.StyleFlexDirection(FlexDirection.Column);
            this.StyleFlexGrow(1);
        }
    }
}