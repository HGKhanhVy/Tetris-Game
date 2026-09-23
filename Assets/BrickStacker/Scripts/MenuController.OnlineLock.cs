using UnityEngine;

namespace BrickStacker
{
    public partial class MenuController : MonoBehaviour
    {
        // 1 VS 1 is held back for the first store release: the menu button is not built at all, so
        // the store reviewers never see a half-finished mode. Set to false to bring the mode back;
        // everything behind the button is left intact.
        static readonly bool IsOnlineModeLocked = true;
    }
}
