using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// 키 설정 UI에서 바꿀 수 있는 게임 입력 항목입니다.
    /// enum 값 이름은 PlayerPrefs 저장 키에도 쓰이므로 이름을 바꾸면 기존 저장값과 끊길 수 있습니다.
    /// </summary>
    public enum KeyBindingAction
    {
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
        Inventory,
        Search,
        Pickup,
        QuickSlot1,
        QuickSlot2,
        QuickSlot3,
        QuickSlot4,
        QuickSlot5,
        QuickSlot6,
        QuickSlot7,
        QuickSlot8,
        QuickSlot9,
        QuickSlot10,
        UseHealthPotion,
        Buy
    }

    /// <summary>
    /// 게임 전체 키 설정과 자동 줍기 설정을 한 곳에서 관리합니다.
    /// 실제 입력 스크립트들은 하드코딩된 KeyCode 대신 이 매니저에서 현재 키를 읽습니다.
    /// </summary>
    public class KeyBindingManager : MonoBehaviour
    {
        // PlayerPrefs에 저장되는 키 이름 접두사입니다.
        private const string KeyPrefix = "KeyBinding.";
        private const string AutoPickupKey = "KeyBinding.AutoPickup";

        // 현재 실행 중인 키 설정값입니다. EnsureLoaded()에서 PlayerPrefs 값을 읽어 채웁니다.
        private static readonly Dictionary<KeyBindingAction, KeyCode> Bindings = new();

        // 저장값이 없을 때 사용하는 기본 키입니다.
        private static readonly Dictionary<KeyBindingAction, KeyCode> Defaults = new()
        {
            { KeyBindingAction.MoveUp, KeyCode.W },
            { KeyBindingAction.MoveDown, KeyCode.S },
            { KeyBindingAction.MoveLeft, KeyCode.A },
            { KeyBindingAction.MoveRight, KeyCode.D },
            { KeyBindingAction.Inventory, KeyCode.Tab },
            { KeyBindingAction.Search, KeyCode.F },
            { KeyBindingAction.Pickup, KeyCode.Space },
            { KeyBindingAction.QuickSlot1, KeyCode.Alpha1 },
            { KeyBindingAction.QuickSlot2, KeyCode.Alpha2 },
            { KeyBindingAction.QuickSlot3, KeyCode.Alpha3 },
            { KeyBindingAction.QuickSlot4, KeyCode.Alpha4 },
            { KeyBindingAction.QuickSlot5, KeyCode.Alpha5 },
            { KeyBindingAction.QuickSlot6, KeyCode.Alpha6 },
            { KeyBindingAction.QuickSlot7, KeyCode.Alpha7 },
            { KeyBindingAction.QuickSlot8, KeyCode.Alpha8 },
            { KeyBindingAction.QuickSlot9, KeyCode.Alpha9 },
            { KeyBindingAction.QuickSlot10, KeyCode.Alpha0 },
            { KeyBindingAction.UseHealthPotion, KeyCode.Q },
            { KeyBindingAction.Buy, KeyCode.E }
        };

        private static bool _loaded;
        private static bool _autoPickupEnabled;

        // 키 변경 대기/취소 직후 같은 프레임에 게임 입력이 같이 실행되지 않도록 기록합니다.
        private static int _inputCapturedFrame = -1;

        /// <summary>특정 액션의 키가 변경됐을 때 UI 갱신용으로 발생합니다.</summary>
        public static event Action<KeyBindingAction, KeyCode> OnBindingChanged;

        /// <summary>자동 줍기 ON/OFF가 변경됐을 때 UI와 줍기 로직 갱신용으로 발생합니다.</summary>
        public static event Action<bool> OnAutoPickupChanged;

        /// <summary>현재 키 변경 입력을 기다리는 중이면 true입니다.</summary>
        public static bool InputCaptureActive { get; private set; }

        /// <summary>자동 줍기 사용 여부입니다. 기본값은 false라서 수동 줍기부터 시작합니다.</summary>
        public static bool AutoPickupEnabled
        {
            get
            {
                EnsureLoaded();
                return _autoPickupEnabled;
            }
        }

        /// <summary>
        /// 키 변경 대기 중이거나 방금 키 변경 입력을 처리한 프레임이면 true입니다.
        /// 플레이어 이동, 줍기, 퀵슬롯 같은 게임 입력은 이 값이 true일 때 쉬어야 합니다.
        /// </summary>
        public static bool ShouldBlockGameplayInput =>
            InputCaptureActive || _inputCapturedFrame == Time.frameCount;

        private void Awake()
        {
            EnsureLoaded();
        }

        /// <summary>지정한 액션의 현재 키를 반환합니다.</summary>
        public static KeyCode GetKey(KeyBindingAction action)
        {
            EnsureLoaded();
            return Bindings.TryGetValue(action, out KeyCode key) ? key : GetDefaultKey(action);
        }

        /// <summary>퀵슬롯 index(0~9)를 키 설정 액션으로 변환합니다.</summary>
        public static KeyBindingAction GetQuickSlotAction(int index)
        {
            return index switch
            {
                0 => KeyBindingAction.QuickSlot1,
                1 => KeyBindingAction.QuickSlot2,
                2 => KeyBindingAction.QuickSlot3,
                3 => KeyBindingAction.QuickSlot4,
                4 => KeyBindingAction.QuickSlot5,
                5 => KeyBindingAction.QuickSlot6,
                6 => KeyBindingAction.QuickSlot7,
                7 => KeyBindingAction.QuickSlot8,
                8 => KeyBindingAction.QuickSlot9,
                _ => KeyBindingAction.QuickSlot10
            };
        }

        /// <summary>
        /// 해당 키를 액션에 배정할 수 있는지 확인합니다.
        /// 현재 정책은 이동/인벤/조사/줍기/퀵슬롯/포션 키 전체 중복 금지입니다.
        /// </summary>
        public static bool CanAssignKey(KeyBindingAction action, KeyCode key)
        {
            EnsureLoaded();
            if (!IsBindableKeyboardKey(key))
                return false;

            foreach (KeyValuePair<KeyBindingAction, KeyCode> pair in Bindings)
            {
                if (pair.Key != action && pair.Value == key)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 키 중복 검사를 통과하면 액션 키를 변경하고 PlayerPrefs에 저장합니다.
        /// 실패하면 false를 반환하므로 UI에서 중복 메시지를 보여주면 됩니다.
        /// </summary>
        public static bool TrySetKey(KeyBindingAction action, KeyCode key)
        {
            EnsureLoaded();
            if (!CanAssignKey(action, key))
                return false;

            Bindings[action] = key;
            PlayerPrefs.SetInt(GetPrefsKey(action), (int)key);
            PlayerPrefs.Save();
            OnBindingChanged?.Invoke(action, key);
            return true;
        }

        /// <summary>자동 줍기 ON/OFF를 변경하고 PlayerPrefs에 저장합니다.</summary>
        public static void SetAutoPickupEnabled(bool enabled)
        {
            EnsureLoaded();
            if (_autoPickupEnabled == enabled)
                return;

            _autoPickupEnabled = enabled;
            PlayerPrefs.SetInt(AutoPickupKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            OnAutoPickupChanged?.Invoke(enabled);
        }

        /// <summary>모든 키와 자동 줍기 설정을 기본값으로 되돌립니다.</summary>
        public static void ResetToDefaults()
        {
            Bindings.Clear();
            foreach (KeyValuePair<KeyBindingAction, KeyCode> pair in Defaults)
            {
                Bindings[pair.Key] = pair.Value;
                PlayerPrefs.SetInt(GetPrefsKey(pair.Key), (int)pair.Value);
                OnBindingChanged?.Invoke(pair.Key, pair.Value);
            }

            _autoPickupEnabled = false;
            PlayerPrefs.SetInt(AutoPickupKey, 0);
            PlayerPrefs.Save();
            OnAutoPickupChanged?.Invoke(false);
            _loaded = true;
        }

        /// <summary>키 변경 UI가 다음 키 입력을 기다리기 시작할 때 호출합니다.</summary>
        public static void BeginInputCapture()
        {
            InputCaptureActive = true;
            _inputCapturedFrame = Time.frameCount;
        }

        /// <summary>키 변경 UI가 입력 대기 상태를 끝낼 때 호출합니다.</summary>
        public static void EndInputCapture()
        {
            InputCaptureActive = false;
            _inputCapturedFrame = Time.frameCount;
        }

        /// <summary>이번 프레임에 키 변경 입력이 처리됐는지 확인합니다.</summary>
        public static bool WasInputCapturedThisFrame()
        {
            return _inputCapturedFrame == Time.frameCount;
        }

        /// <summary>저장된 키 설정을 한 번만 로드합니다.</summary>
        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            Bindings.Clear();
            foreach (KeyValuePair<KeyBindingAction, KeyCode> pair in Defaults)
            {
                int savedValue = PlayerPrefs.GetInt(GetPrefsKey(pair.Key), (int)pair.Value);
                Bindings[pair.Key] = Enum.IsDefined(typeof(KeyCode), savedValue)
                    ? (KeyCode)savedValue
                    : pair.Value;
            }

            _autoPickupEnabled = PlayerPrefs.GetInt(AutoPickupKey, 0) == 1;
            _loaded = true;
        }

        /// <summary>기본 키를 반환합니다. 등록되지 않은 액션이면 KeyCode.None입니다.</summary>
        private static KeyCode GetDefaultKey(KeyBindingAction action)
        {
            return Defaults.TryGetValue(action, out KeyCode key) ? key : KeyCode.None;
        }

        /// <summary>PlayerPrefs 저장에 사용할 액션별 키 이름을 만듭니다.</summary>
        private static string GetPrefsKey(KeyBindingAction action)
        {
            return KeyPrefix + action;
        }

        /// <summary>
        /// 키 설정으로 받을 수 있는 키인지 확인합니다.
        /// ESC는 취소키로 쓰기 때문에 배정하지 않고, 마우스/조이스틱 입력도 v1에서는 제외합니다.
        /// </summary>
        private static bool IsBindableKeyboardKey(KeyCode key)
        {
            if (key == KeyCode.None || key == KeyCode.Escape)
                return false;

            string keyName = key.ToString();
            return !keyName.StartsWith("Mouse", StringComparison.Ordinal)
                && !keyName.StartsWith("Joystick", StringComparison.Ordinal);
        }
    }
}
