using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PrototypeRT
{
    // InventoryPanel 안의 큰 탭, 시스템 하위 탭, 확인창을 한 곳에서 전환한다.
    public class UIManager : MonoBehaviour
    {
        public enum MainTab
        {
            Inventory,
            TuckSung,
            System
        }

        public enum SystemTab
        {
            None,
            KeySetting,
            AudioSetting
        }

        [Header("Window Root")]
        [Tooltip("Tab으로 열고 닫는 전체 UI 창 루트. 예: InventoryPanel")]
        [SerializeField] private GameObject windowRoot;

        [Header("Main Buttons")]
        [Tooltip("인벤토리 탭을 여는 오른쪽 버튼.")]
        [SerializeField] private Button inventoryButton;
        [Tooltip("특성 탭을 여는 오른쪽 버튼.")]
        [SerializeField] private Button tuckSungButton;
        [Tooltip("시스템 탭을 여는 오른쪽 버튼.")]
        [SerializeField] private Button systemButton;

        [Header("Main Panels")]
        [Tooltip("인벤토리 내용 패널. InventoryBT를 누르면 이것만 켜진다.")]
        [SerializeField] private GameObject inventoryPanel;
        [Tooltip("특성 내용 패널. TuckSungBT를 누르면 이것만 켜진다.")]
        [SerializeField] private GameObject tuckSungPanel;
        [Tooltip("시스템 내용 패널. SystemBT를 누르면 이것만 켜진다.")]
        [SerializeField] private GameObject systemPanel;

        [Header("System Buttons")]
        [Tooltip("키 설정 항목이 Button 컴포넌트일 때 연결.")]
        [SerializeField] private Button keySettingButton;
        [Tooltip("오디오 설정 항목이 Button 컴포넌트일 때 연결.")]
        [SerializeField] private Button audioSettingButton;
        [Tooltip("키 설정 항목이 Button이 아니라 Text/Image 오브젝트일 때 연결.")]
        [SerializeField] private GameObject keySettingClickTarget;
        [Tooltip("오디오 설정 항목이 Button이 아니라 Text/Image 오브젝트일 때 연결.")]
        [SerializeField] private GameObject audioSettingClickTarget;

        [Header("System Panels")]
        [Tooltip("키 설정 항목을 눌렀을 때 켜지는 패널.")]
        [SerializeField] private GameObject keySettingPanel;
        [Tooltip("오디오 설정 항목을 눌렀을 때 켜지는 패널.")]
        [SerializeField] private GameObject audioSettingPanel;

        [Header("Confirm Panel Open Buttons")]
        [Tooltip("게임 종료 확인창을 여는 버튼. Button 컴포넌트가 있으면 여기에 연결.")]
        [SerializeField] private Button gameExitOpenButton;
        [Tooltip("게임 종료 확인창을 여는 클릭 대상. Text/Image만 있을 때 연결.")]
        [SerializeField] private GameObject gameExitOpenClickTarget;
        [Tooltip("메인 화면 확인창을 여는 버튼. Button 컴포넌트가 있으면 여기에 연결.")]
        [SerializeField] private Button goToMainOpenButton;
        [Tooltip("메인 화면 확인창을 여는 클릭 대상. Text/Image만 있을 때 연결.")]
        [SerializeField] private GameObject goToMainOpenClickTarget;

        [Header("Confirm Panels")]
        [Tooltip("게임 종료 확인창 패널.")]
        [SerializeField] private GameObject gameExitPanel;
        [Tooltip("메인 화면 이동 확인창 패널.")]
        [SerializeField] private GameObject goToMainPanel;

        [Header("Confirm Cancel Buttons")]
        [Tooltip("게임 종료 확인창의 취소 버튼. 누르면 확인창만 닫힌다.")]
        [SerializeField] private Button gameExitCancelButton;
        [Tooltip("메인 화면 이동 확인창의 취소 버튼. 누르면 확인창만 닫힌다.")]
        [SerializeField] private Button goToMainCancelButton;

        [Header("Controllers")]
        [Tooltip("인벤토리 탭이 열렸을 때만 드래그/하이라이트 입력을 허용할 컨트롤러.")]
        [SerializeField] private InventoryController inventoryController;

        [Header("Defaults")]
        [Tooltip("전체 창이 처음 열리거나 Tab으로 다시 켜질 때 기본으로 보여줄 메인 탭.")]
        [SerializeField] private MainTab defaultMainTab = MainTab.Inventory;
        [Tooltip("시스템 탭에 처음 들어갔을 때 기본으로 보여줄 하위 탭.")]
        [SerializeField] private SystemTab defaultSystemTab = SystemTab.KeySetting;

        private MainTab _currentMainTab;
        private SystemTab _currentSystemTab;
        private bool _lastWindowActive;
        private GameObject _topModalPanel;

        private void Awake()
        {
            _currentMainTab = defaultMainTab;
            _currentSystemTab = defaultSystemTab;

            RegisterButton(inventoryButton, OpenInventory);
            RegisterButton(tuckSungButton, OpenTuckSung);
            RegisterButton(systemButton, OpenSystem);
            RegisterButton(keySettingButton, OpenKeySetting);
            RegisterButton(audioSettingButton, OpenAudioSetting);
            RegisterButton(gameExitOpenButton, OpenGameExitPanel);
            RegisterButton(goToMainOpenButton, OpenGoToMainPanel);
            RegisterButton(gameExitCancelButton, CloseTopModalPanel);
            RegisterButton(goToMainCancelButton, CloseTopModalPanel);

            RegisterClickTarget(keySettingClickTarget, OpenKeySetting);
            RegisterClickTarget(audioSettingClickTarget, OpenAudioSetting);
            RegisterClickTarget(gameExitOpenClickTarget, OpenGameExitPanel);
            RegisterClickTarget(goToMainOpenClickTarget, OpenGoToMainPanel);
        }

        private void Start()
        {
            CloseAllModalPanels();
            OpenMainTab(defaultMainTab);
            _lastWindowActive = IsWindowActive();
            SyncInventoryInput();
        }

        private void LateUpdate()
        {
            if (KeyBindingManager.InputCaptureActive || KeyBindingManager.WasInputCapturedThisFrame())
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                CloseTopUI();

            bool isWindowActive = IsWindowActive();
            if (isWindowActive == _lastWindowActive) return;

            _lastWindowActive = isWindowActive;

            if (isWindowActive)
                OpenInventory();
            else
                CloseAllModalPanels();

            SyncInventoryInput();
        }

        public void OpenInventory()
        {
            OpenMainTab(MainTab.Inventory);
        }

        public void OpenTuckSung()
        {
            OpenMainTab(MainTab.TuckSung);
        }

        public void OpenSystem()
        {
            OpenMainTab(MainTab.System);
        }

        public void OpenKeySetting()
        {
            OpenSystemTab(SystemTab.KeySetting);
        }

        public void OpenAudioSetting()
        {
            OpenSystemTab(SystemTab.AudioSetting);
        }

        public void OpenGameExitPanel()
        {
            OpenModalPanel(gameExitPanel);
        }

        public void OpenGoToMainPanel()
        {
            OpenModalPanel(goToMainPanel);
        }

        public void CloseTopModalPanel()
        {
            if (_topModalPanel != null && _topModalPanel.activeSelf)
            {
                _topModalPanel.SetActive(false);
                _topModalPanel = null;
                SyncInventoryInput();
                return;
            }

            CloseAllModalPanels();
            SyncInventoryInput();
        }

        public void OpenMainTab(MainTab tab)
        {
            _currentMainTab = tab;

            SetActive(inventoryPanel, tab == MainTab.Inventory);
            SetActive(tuckSungPanel, tab == MainTab.TuckSung);
            SetActive(systemPanel, tab == MainTab.System);

            if (tab == MainTab.System && _currentSystemTab == SystemTab.None)
                _currentSystemTab = defaultSystemTab;

            if (tab == MainTab.System)
                OpenSystemTab(_currentSystemTab);
            else
                HideSystemSubPanels();

            SyncInventoryInput();
        }

        public void OpenSystemTab(SystemTab tab)
        {
            _currentSystemTab = tab;

            SetActive(keySettingPanel, tab == SystemTab.KeySetting);
            SetActive(audioSettingPanel, tab == SystemTab.AudioSetting);
        }

        private void OpenModalPanel(GameObject panel)
        {
            if (panel == null) return;

            if (!IsWindowActive() && windowRoot != null)
                windowRoot.SetActive(true);

            CloseAllModalPanels();
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            _topModalPanel = panel;
            SyncInventoryInput();
        }

        private void CloseTopUI()
        {
            if (_topModalPanel != null && _topModalPanel.activeSelf)
            {
                CloseTopModalPanel();
                return;
            }

            if (IsWindowActive() && windowRoot != null)
            {
                windowRoot.SetActive(false);
                CloseAllModalPanels();
                SyncInventoryInput();
            }
        }

        private void CloseAllModalPanels()
        {
            SetActive(gameExitPanel, false);
            SetActive(goToMainPanel, false);
            _topModalPanel = null;
        }

        private void SyncInventoryInput()
        {
            bool canUseInventory = IsWindowActive() && _currentMainTab == MainTab.Inventory && _topModalPanel == null;
            inventoryController?.SetInventoryOpen(canUseInventory);
        }

        private bool IsWindowActive()
        {
            return windowRoot == null || windowRoot.activeInHierarchy;
        }

        private void HideSystemSubPanels()
        {
            SetActive(keySettingPanel, false);
            SetActive(audioSettingPanel, false);
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null && target.activeSelf != value)
                target.SetActive(value);
        }

        private static void RegisterButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(action);
        }

        private static void RegisterClickTarget(GameObject target, UnityEngine.Events.UnityAction action)
        {
            if (target == null) return;

            if (target.TryGetComponent(out Button button))
            {
                button.onClick.AddListener(action);
                return;
            }

            EventTrigger trigger = target.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = target.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };
            entry.callback.AddListener(_ => action.Invoke());
            trigger.triggers.Add(entry);
        }
    }
}
