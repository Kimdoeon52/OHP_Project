using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PrototypeRT
{
    /// <summary>
    /// UI 탭 메뉴의 전환 및 하위 패널 상태를 중앙에서 관리하는 매니저입니다.
    /// 인벤토리, 특성, 시스템 등 주요 탭의 열림/닫힘 상태를 제어합니다.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        /// <summary>
        /// 상단에 위치한 메인 탭 종류
        /// </summary>
        public enum MainTab
        {
            Inventory,
            TuckSung,
            System
        }

        /// <summary>
        /// 시스템 탭 내부의 하위 탭 종류
        /// </summary>
        public enum SystemTab
        {
            None,
            KeySetting,
            AudioSetting
        }

        [Header("Window")]
        [Tooltip("전체 UI 창의 최상위 부모 객체입니다.")]
        [SerializeField] private GameObject windowRoot;

        [Header("Main Buttons")]
        [Tooltip("인벤토리 탭을 여는 버튼입니다.")]
        [SerializeField] private Button inventoryButton;
        [Tooltip("특성 탭을 여는 버튼입니다.")]
        [SerializeField] private Button tuckSungButton;
        [Tooltip("시스템 탭을 여는 버튼입니다.")]
        [SerializeField] private Button systemButton;

        [Header("Main Panels")]
        [Tooltip("인벤토리 관련 내용이 표시될 패널입니다.")]
        [SerializeField] private GameObject inventoryPanel;
        [Tooltip("특성 관련 내용이 표시될 패널입니다.")]
        [SerializeField] private GameObject tuckSungPanel;
        [Tooltip("시스템(설정 등) 관련 내용이 표시될 패널입니다.")]
        [SerializeField] private GameObject systemPanel;

        [Header("System Buttons")]
        [Tooltip("키 설정 하위 탭을 여는 버튼입니다.")]
        [SerializeField] private Button keySettingButton;
        [Tooltip("오디오 설정 하위 탭을 여는 버튼입니다.")]
        [SerializeField] private Button audioSettingButton;
        
        [Tooltip("키 설정 탭 버튼 대신 클릭 이벤트를 받을 일반 GameObject입니다.")]
        [SerializeField] private GameObject keySettingClickTarget;
        [Tooltip("오디오 설정 탭 버튼 대신 클릭 이벤트를 받을 일반 GameObject입니다.")]
        [SerializeField] private GameObject audioSettingClickTarget;

        [Header("System Panels")]
        [Tooltip("키 설정 내용이 표시될 패널입니다.")]
        [SerializeField] private GameObject keySettingPanel;
        [Tooltip("오디오 설정 내용이 표시될 패널입니다.")]
        [SerializeField] private GameObject audioSettingPanel;

        [Header("Controllers")]
        [Tooltip("인벤토리 활성화 여부를 전달할 인벤토리 컨트롤러입니다.")]
        [SerializeField] private InventoryController inventoryController;

        [Header("Defaults")]
        [Tooltip("처음 UI가 켜질 때 기본으로 표시할 메인 탭입니다.")]
        [SerializeField] private MainTab defaultMainTab = MainTab.Inventory;
        [Tooltip("처음 시스템 탭이 열릴 때 기본으로 표시할 하위 탭입니다.")]
        [SerializeField] private SystemTab defaultSystemTab = SystemTab.KeySetting;

        private MainTab _currentMainTab;
        private SystemTab _currentSystemTab;
        private bool _lastWindowActive;

        private void Awake()
        {
            // 초기 탭 설정
            _currentMainTab = defaultMainTab;
            _currentSystemTab = defaultSystemTab;

            // 메인 탭 버튼 이벤트 등록
            RegisterButton(inventoryButton, OpenInventory);
            RegisterButton(tuckSungButton, OpenTuckSung);
            RegisterButton(systemButton, OpenSystem);
            
            // 시스템 탭 내부 버튼 이벤트 등록
            RegisterButton(keySettingButton, OpenKeySetting);
            RegisterButton(audioSettingButton, OpenAudioSetting);

            // Button 컴포넌트가 없는 텍스트/이미지 영역 클릭 처리
            RegisterClickTarget(keySettingClickTarget, OpenKeySetting);
            RegisterClickTarget(audioSettingClickTarget, OpenAudioSetting);
        }

        private void Start()
        {
            // 시작 시 기본 탭을 화면에 띄웁니다.
            OpenMainTab(_currentMainTab);
            
            // 초기 창 활성화 상태를 기록하고 인벤토리 컨트롤러에 동기화합니다.
            _lastWindowActive = IsWindowActive();
            SyncInventoryInput();
        }

        private void LateUpdate()
        {
            // 창이 닫히거나 열리는 상태 변화를 감지합니다.
            bool isWindowActive = IsWindowActive();
            if (isWindowActive == _lastWindowActive) return;

            // 상태가 변했다면 인벤토리 입력 처리에 알립니다.
            _lastWindowActive = isWindowActive;
            SyncInventoryInput();
        }

        /// <summary>
        /// 인벤토리 탭을 엽니다.
        /// </summary>
        public void OpenInventory()
        {
            OpenMainTab(MainTab.Inventory);
        }

        /// <summary>
        /// 특성 탭을 엽니다.
        /// </summary>
        public void OpenTuckSung()
        {
            OpenMainTab(MainTab.TuckSung);
        }

        /// <summary>
        /// 시스템 탭을 엽니다.
        /// </summary>
        public void OpenSystem()
        {
            OpenMainTab(MainTab.System);
        }

        /// <summary>
        /// 시스템 - 키 설정 탭을 엽니다.
        /// </summary>
        public void OpenKeySetting()
        {
            OpenSystemTab(SystemTab.KeySetting);
        }

        /// <summary>
        /// 시스템 - 오디오 설정 탭을 엽니다.
        /// </summary>
        public void OpenAudioSetting()
        {
            OpenSystemTab(SystemTab.AudioSetting);
        }

        /// <summary>
        /// 지정한 메인 탭을 활성화하고 나머지 패널을 비활성화합니다.
        /// </summary>
        public void OpenMainTab(MainTab tab)
        {
            _currentMainTab = tab;

            // 요청된 탭의 패널만 켜고 나머지는 끕니다.
            SetActive(inventoryPanel, tab == MainTab.Inventory);
            SetActive(tuckSungPanel, tab == MainTab.TuckSung);
            SetActive(systemPanel, tab == MainTab.System);

            // 시스템 탭을 열었는데 지정된 하위 탭이 없다면 기본값으로 설정합니다.
            if (tab == MainTab.System && _currentSystemTab == SystemTab.None)
                _currentSystemTab = defaultSystemTab;

            // 시스템 탭이면 내부 패널도 열어주고, 아니면 전부 숨깁니다.
            if (tab == MainTab.System)
                OpenSystemTab(_currentSystemTab);
            else
                HideSystemSubPanels();

            // 인벤토리 탭 열림 여부가 변경되었을 수 있으므로 동기화합니다.
            SyncInventoryInput();
        }

        /// <summary>
        /// 지정한 시스템 하위 탭을 활성화합니다.
        /// </summary>
        public void OpenSystemTab(SystemTab tab)
        {
            _currentSystemTab = tab;

            SetActive(keySettingPanel, tab == SystemTab.KeySetting);
            SetActive(audioSettingPanel, tab == SystemTab.AudioSetting);
        }

        /// <summary>
        /// 현재 창이 열려 있고, 메인 탭이 인벤토리일 때만 인벤토리 상호작용을 허용하도록 컨트롤러에 알립니다.
        /// </summary>
        private void SyncInventoryInput()
        {
            bool canUseInventory = IsWindowActive() && _currentMainTab == MainTab.Inventory;
            inventoryController?.SetInventoryOpen(canUseInventory);
        }

        /// <summary>
        /// UI 창 전체가 화면에 띄워져 있는지 확인합니다.
        /// </summary>
        private bool IsWindowActive()
        {
            return windowRoot == null || windowRoot.activeInHierarchy;
        }

        /// <summary>
        /// GameObject의 활성화 상태가 다를 때만 SetActive를 호출하여 성능을 최적화합니다.
        /// </summary>
        private static void SetActive(GameObject target, bool value)
        {
            if (target != null && target.activeSelf != value)
                target.SetActive(value);
        }

        /// <summary>
        /// 버튼의 onClick 이벤트에 메서드를 안전하게 연결합니다.
        /// </summary>
        private static void RegisterButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(action);
        }

        /// <summary>
        /// Button 컴포넌트가 없는 대상도 포인터 클릭 이벤트를 받을 수 있도록 EventTrigger를 추가하고 연결합니다.
        /// </summary>
        private static void RegisterClickTarget(GameObject target, UnityEngine.Events.UnityAction action)
        {
            if (target == null) return;

            // 이미 버튼이 있다면 버튼 이벤트를 사용합니다.
            if (target.TryGetComponent(out Button button))
            {
                button.onClick.AddListener(action);
                return;
            }

            // 없다면 EventTrigger를 생성하거나 가져와서 포인터 클릭 이벤트를 연결합니다.
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

        /// <summary>
        /// 시스템 하위 탭 패널들을 모두 숨깁니다.
        /// </summary>
        private void HideSystemSubPanels()
        {
            SetActive(keySettingPanel, false);
            SetActive(audioSettingPanel, false);
        }
    }
}
