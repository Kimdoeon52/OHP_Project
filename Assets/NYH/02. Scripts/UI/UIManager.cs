using System.Collections.Generic;
using UnityEngine;

namespace PrototypeRT
{
    public class UIManager : Singleton<UIManager>
    {
        [Header("Panels")]
        [SerializeField] private ManagedUIPanel[] panels;
        [SerializeField] private UIPanelId settingsPanelId = UIPanelId.Settings;

        [Header("Input")]
        [SerializeField] private KeyCode escapeKey = KeyCode.Escape;

        private readonly Dictionary<UIPanelId, ManagedUIPanel> panelMap = new();
        private ManagedUIPanel currentPanel;
        private bool timerWasRunningBeforePause;
        private bool hasPausedTimer;

        public bool BlocksGameplayInput => currentPanel != null && currentPanel.BlockGameplayInput;
        public UIPanelId? CurrentPanelId => currentPanel != null ? currentPanel.PanelId : null;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            CachePanels();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(escapeKey))
                return;

            if (currentPanel != null)
            {
                CloseCurrent();
                return;
            }

            if (panelMap.ContainsKey(settingsPanelId))
                Open(settingsPanelId);
        }

        public void Toggle(UIPanelId panelId)
        {
            if (currentPanel != null && currentPanel.PanelId == panelId)
            {
                CloseCurrent();
                return;
            }

            Open(panelId);
        }

        public void Open(UIPanelId panelId)
        {
            if (!panelMap.TryGetValue(panelId, out ManagedUIPanel nextPanel) || nextPanel == null)
            {
                Debug.LogWarning($"UIManager: {panelId} 패널이 등록되지 않았습니다.");
                return;
            }

            if (currentPanel == nextPanel)
                return;

            CloseCurrent();
            currentPanel = nextPanel;
            ApplyTimerPauseIfNeeded(nextPanel);
            nextPanel.OpenPanel();
        }

        public void CloseCurrent()
        {
            if (currentPanel == null)
                return;

            ManagedUIPanel closingPanel = currentPanel;
            currentPanel = null;

            closingPanel.ClosePanel();
            RestoreTimerIfNeeded(closingPanel);
        }

        private void CachePanels()
        {
            panelMap.Clear();

            if (panels == null || panels.Length == 0)
                panels = FindObjectsByType<ManagedUIPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (ManagedUIPanel panel in panels)
            {
                if (panel == null)
                    continue;

                if (panelMap.ContainsKey(panel.PanelId))
                {
                    Debug.LogWarning($"UIManager: {panel.PanelId} 패널이 중복 등록되어 뒤에 발견된 패널은 무시합니다.");
                    continue;
                }

                panelMap.Add(panel.PanelId, panel);

                if (panel.IsOpen)
                    currentPanel = panel;
            }
        }

        private void ApplyTimerPauseIfNeeded(ManagedUIPanel panel)
        {
            if (!panel.PauseTimer || TimeSystem.Instance == null)
                return;

            // Safe Room처럼 원래 타이머가 멈춰 있던 상태는 설정창을 닫아도 다시 시작하지 않기 위해 이전 실행 상태를 저장한다.
            timerWasRunningBeforePause = TimeSystem.Instance.IsRunning;
            hasPausedTimer = true;
            TimeSystem.Instance.StopTimer();
        }

        private void RestoreTimerIfNeeded(ManagedUIPanel panel)
        {
            if (!panel.PauseTimer || !hasPausedTimer)
                return;

            if (timerWasRunningBeforePause && TimeSystem.Instance != null)
                TimeSystem.Instance.StartTimer();

            hasPausedTimer = false;
            timerWasRunningBeforePause = false;
        }
    }
}
