using System.Collections.Generic;
using UnityEngine;

namespace PrototypeRT
{
    // 로비의 모든 팝업/설정 창을 ESC 하나로 닫는 단일 처리기.
    // 각 창은 자체 ESC 처리를 두지 않고 이 목록에 등록만 한다.
    public class LobbyEscController : MonoBehaviour
    {
        [Tooltip("ESC로 닫을 창들. 위에 있을수록 우선. 겹쳐 있으면 위에 있는 것부터 닫힌다.")]
        [SerializeField] private List<GameObject> panelsTopPriorityFirst = new List<GameObject>();

        [Tooltip("창을 닫을 키")]
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        private void Update()
        {
            if (!Input.GetKeyDown(closeKey))
                return;

            // 키 재설정 대기 중이면 ESC는 재설정 취소용이므로 창 닫기로 쓰지 않는다.
            if (KeyBindingManager.InputCaptureActive || KeyBindingManager.WasInputCapturedThisFrame())
                return;

            CloseTopmostOpenPanel();
        }

        // 목록에서 처음으로 켜져 있는 창 하나만 닫는다.
        private void CloseTopmostOpenPanel()
        {
            for (int i = 0; i < panelsTopPriorityFirst.Count; i++)
            {
                GameObject panel = panelsTopPriorityFirst[i];
                if (panel == null || !panel.activeInHierarchy)
                    continue;

                panel.SetActive(false);
                return;
            }
        }
    }
}
