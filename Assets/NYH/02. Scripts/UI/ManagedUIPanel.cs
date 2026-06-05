using UnityEngine;

namespace PrototypeRT
{
    public class ManagedUIPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private UIPanelId panelId;
        [SerializeField] private GameObject panelRoot;

        [Header("Behavior")]
        [SerializeField] private bool pauseTimer;
        [SerializeField] private bool blockGameplayInput = true;
        [SerializeField] private bool refreshOnOpen;
        [SerializeField] private PlayerStatsView statsView;

        public UIPanelId PanelId => panelId;
        public bool PauseTimer => pauseTimer;
        public bool BlockGameplayInput => blockGameplayInput;
        public virtual bool IsOpen => ResolvePanelRoot().activeSelf;

        public virtual void OpenPanel()
        {
            GameObject root = ResolvePanelRoot();
            root.SetActive(true);

            if (refreshOnOpen)
            {
                // 패널을 열 때 최신 스탯을 다시 그려야 인벤토리/특성창 수치가 이전 프레임 값으로 남지 않는다.
                if (statsView == null)
                    statsView = FindFirstObjectByType<PlayerStatsView>();

                statsView?.Refresh();
            }
        }

        public virtual void ClosePanel()
        {
            ResolvePanelRoot().SetActive(false);
        }

        protected GameObject ResolvePanelRoot()
        {
            // 별도 루트가 없는 단순 패널은 이 컴포넌트가 붙은 오브젝트 자체를 패널 루트로 사용한다.
            return panelRoot != null ? panelRoot : gameObject;
        }
    }
}
