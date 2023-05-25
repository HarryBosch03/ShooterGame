using Bosch.Player;
using TMPro;
using UnityEngine;

namespace Bosch.UI
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public sealed class Speedometer : MonoBehaviour
    {
        private string template;
        private PlayerAvatar player;
        private TMP_Text text;

        private void Awake()
        {
            player = GetComponentInParent<PlayerAvatar>();
            text = GetComponent<TMP_Text>();

            template = text.text;
        }

        private void Update()
        {
            text.text = string.Format(template, Mathf.Round(player.Movement.MoveSpeed));
        }
    }
}
