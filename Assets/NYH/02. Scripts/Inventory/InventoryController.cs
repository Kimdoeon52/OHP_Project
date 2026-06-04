using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// ?몃깽?좊━ UI???꾩껜 ?낅젰 ?먮쫫??愿由ы븯??而⑦듃濡ㅻ윭?낅땲??
    /// ?꾩씠???앹꽦/異붽?/??젣, 留덉슦???쒕옒洹? 諛곗튂, ?섏씠?쇱씠??媛깆떊????怨녹뿉??議곗쑉?⑸땲??
    /// 
    /// ?뚮젅?댁뼱 ?몃깽?좊━ ?쒖뒪?쒖쓣 珥앷큵?섎뒗 ?듭떖 ?대옒?ㅼ엯?덈떎.
    /// ?꾩씠???좏깮 諛??쒕옒洹????쒕∼, ?щ’ 媛??대룞, ?좉퇋 ?꾩씠???띾뱷 ??鍮덉뭏 ?먮룞 諛곗튂 ??
    /// ?몃깽?좊━ ?댁뿉??諛쒖깮?섎뒗 紐⑤뱺 ?곹샇?묒슜怨??곗씠???먮쫫??以묒븰?먯꽌 ?쒖뼱?⑸땲??
    /// </summary>
    [RequireComponent(typeof(InventoryHighlight))]
    public class InventoryController : MonoBehaviour
    {
        [Header("UI Prefabs")]
        [Tooltip("?몃깽?좊━ 移몄뿉 ?앹꽦???꾩씠??UI ?꾨━?뱀엯?덈떎. InventoryItem 而댄룷?뚰듃? Image媛 遺숈뼱 ?덉뼱???⑸땲??")]
        [SerializeField] private InventoryItem itemPrefab;

        [Tooltip("?꾩씠??UI瑜??앹꽦??Canvas Transform?낅땲?? 鍮꾩썙?먮㈃ 遺紐?Canvas瑜??먮룞?쇰줈 李얠뒿?덈떎.")]
        [SerializeField] private Transform canvasTransform;

        [Tooltip("?꾩씠???먮룞 異붽????ъ슜??湲곕낯 ?몃깽?좊━ 洹몃━?쒖엯?덈떎. ?듭뒳濡?洹몃━?쒓? ?꾨땶 ??媛諛?洹몃━?쒕? ?곌껐?섏꽭??")]
        [SerializeField] private ItemGrid defaultInventoryGrid;

        [Header("?꾩씠???ъ슜")]
        [Tooltip("?꾩씠?쒖쓣 ?ㅺ퀬 ?ㅼ떆 ?대┃?덉쓣 ???붾툝?대┃?쇰줈 ?몄젙??理쒕? ?쒓컙?낅땲??")]
        [SerializeField, Min(0.05f)] private float doubleClickMaxDelay = 0.25f;

        [Tooltip("?붾툝?대┃ ?꾩씠???ъ슜 遺꾧린瑜??대떦?섎뒗 而⑦듃濡ㅻ윭?낅땲?? 鍮꾩썙?먮㈃ ?고??꾩뿉 ?먮룞?쇰줈 遺숈엯?덈떎.")]
        [SerializeField] private InventoryItemUseController itemUseController;

        [Tooltip("二쇰Ц?????吏?뺢낵 ?ъ쿃 ?ъ뀡 議곗????대떦?섎뒗 而⑦듃濡ㅻ윭?낅땲?? 鍮꾩썙?먮㈃ ?고??꾩뿉 ?먮룞?쇰줈 遺숈엯?덈떎.")]
        [SerializeField] private InventoryTargetingController targetingController;

        [Header("Item Drop")]
        [Tooltip("???곸뿭 諛뽰뿉 ?ㅺ퀬 ?덈뒗 ?꾩씠?쒖쓣 ?볦쑝硫??뚮젅?댁뼱 諛쒕컩??踰꾨┰?덈떎. ?몃깽?좊━ ?꾩껜 ?⑤꼸 RectTransform???곌껐?섏꽭??")]
        [SerializeField] private RectTransform inventoryPanelRect;

        [Tooltip("諛붾떏??踰꾨┛ ?꾩씠?쒓낵 以띻린 ?쒖꽌瑜?愿由ы븯??而⑦듃濡ㅻ윭?낅땲?? 鍮꾩썙?먮㈃ ?ъ뿉???먮룞?쇰줈 李얠뒿?덈떎.")]
        [SerializeField] private GroundItemController groundItemController;

        // ?꾩옱 留덉슦?ㅺ? ?щ씪媛??몃깽?좊━ 洹몃━?쒖엯?덈떎. ?щ윭 洹몃━?쒕? 吏?먰븷 ???덈룄濡??ъ씤??吏꾩엯/?댄깉 ??媛깆떊?⑸땲??
        private ItemGrid _selectedGrid;

        // 留덉슦?ㅻ줈 吏묒뼱???쒕옒洹?以묒씤 ?꾩씠?쒖엯?덈떎. null?대㈃ ?먯뿉 ?ㅺ퀬 ?덈뒗 ?꾩씠?쒖씠 ?녿뒗 ?곹깭?낅땲??
        private InventoryItem _selectedItem;

        // 諛곗튂???꾩튂???대? ?꾩씠?쒖씠 ?섎굹留?寃뱀퀜 ?덉쓣 ?? 援먯껜 ?쒕옒洹몃? ?꾪빐 ?좎떆 蹂닿??⑸땲??
        private InventoryItem _overlapItem;

        // ?쒕옒洹?以묒씤 ?꾩씠?쒖쓽 RectTransform 罹먯떆?낅땲?? 留??꾨젅??GetComponent瑜??쇳븯湲??꾪빐 蹂닿??⑸땲??
        private RectTransform _dragRect;

        private InventoryHighlight _inventoryHighlight;

        // ?뚮젅?댁뼱媛 ?ㅼ젣濡??뚯쑀???몃깽?좊━ ?꾩씠??UI 紐⑸줉?낅땲?? ?먮ℓ/?μ갑 UI ?깆뿉???쎌뼱媛????덉뒿?덈떎.
        private readonly List<InventoryItem> _items = new();

        // ?곸젏/????ㅻⅨ UI媛 ?대졇?????몃깽?좊━ ?대┃ 泥섎━瑜??좉퉸 留됯린 ?꾪븳 ?뚮옒洹몄엯?덈떎.
        private bool _isPointerInputBlocked;

        // ?몃깽?좊━ ?⑤꼸???대젮 ?덈뒗吏 湲곕줉?⑸땲??
        // ?⑤꼸???ロ엺 ?숈븞?먮뒗 ?대┃/?섏씠?쇱씠???쒕옒洹??낅젰??泥섎━?섏? ?딄린 ?꾪빐 ?ъ슜?⑸땲??
        private bool _isInventoryOpen;

        // ?쒕옒洹??쒖옉 ?쒖젏??"?먮옒 移?????ν빀?덈떎.
        // ?붾㈃ 醫뚰몴瑜???ν븯吏 ?딄퀬 ItemGrid + 洹몃━??X/Y瑜???ν븯???댁쑀:
        // Canvas ?댁긽?? ?듭빱, ?ㅼ??쇱씠 ?щ씪???꾩씠?쒖씠 ?ㅼ젣濡??뚯븘媛?????꾩튂??"紐?踰덉㎏ 移??닿린 ?뚮Ц?낅땲??
        private ItemGrid _dragStartGrid;
        private int _dragStartX;
        private int _dragStartY;

        private InventoryItem _lastPickedItem;
        private float _lastPickTime;

        /// <summary>?꾩씠??異붽?/??젣/?щ같移????몃깽?좊━ 援ъ꽦??諛붾????몄텧?⑸땲??</summary>
        public event Action OnInventoryChanged;

        /// <summary>???꾩씠?쒖씠 ?몃깽?좊━???깃났?곸쑝濡??ㅼ뼱?붿쓣 ?? 異붽????꾩씠??UI? ?④퍡 ?몄텧?⑸땲??</summary>
        public event Action<InventoryItem> OnItemAdded;

        public IReadOnlyList<InventoryItem> Items => _items;
        public InventoryItem SelectedItem => _selectedItem;

        public ItemGrid SelectedGrid
        {
            get => _selectedGrid;
            set
            {
                if (_selectedGrid == value) return;

                _selectedGrid = value;
                _inventoryHighlight?.SetParent(value);
            }
        }

        public void ClearSelectedGridIfCurrent(ItemGrid grid)
        {
            if (_selectedGrid != grid) return;
            SelectedGrid = null;
        }

        /// <summary>
        /// 而댄룷?뚰듃媛 耳쒖쭏 ???꾩슂??李몄“瑜?誘몃━ 李얠븘?〓땲??
        /// ?섏씠?쇱씠?? Canvas, 湲곕낯 ItemGrid瑜?以鍮꾪븯??珥덇린???⑥닔?낅땲??
        /// </summary>
        private void Awake()
        {
            _inventoryHighlight = GetComponent<InventoryHighlight>();

            // ?쒕옒洹?以묒씤 ?꾩씠?쒖? 洹몃━??諛뽰뿉?쒕룄 留덉슦?ㅻ? ?곕씪????섎?濡?Canvas 諛붾줈 ?꾨옒???〓땲??
            if (canvasTransform == null)
                canvasTransform = GetComponentInParent<Canvas>()?.transform;

            if (_selectedGrid == null)
                SelectedGrid = ResolveItemGrid();

            ResolveUseControllers();
            ResolveGroundItemController();
        }

        /// <summary>
        /// 留??꾨젅???몃깽?좊━ ?낅젰怨??쒕옒洹??쒖떆瑜?泥섎━?⑸땲??
        /// ?붾쾭洹??꾩씠??異붽?, 醫뚰겢由?吏묎린/諛곗튂, ?섏씠?쇱씠??媛깆떊???ш린???쒖옉?⑸땲??
        /// </summary>
        private void Update()
        {
            // ?몃깽?좊━ ?⑤꼸???ロ? ?덉쑝硫??낅젰???꾨? 臾댁떆?⑸땲??
            // ???곹깭?먯꽌 ?대┃???ㅼ뼱?ㅻ㈃ ?ロ엺 UI???꾩씠?쒖쓣 吏묐뒗 臾몄젣媛 ?앷만 ???덉뒿?덈떎.
            if (!_isInventoryOpen)
            {
                _inventoryHighlight.Show(false);
                return;
            }

            // ?ㅺ퀬 ?덈뒗 ?꾩씠???대?吏???낅젰 媛???щ?? ?곴??놁씠 留덉슦?ㅻ? ?곕씪?ㅺ쾶 ?〓땲??
            DragSelectedItemIcon();
            RefreshSelectedGridUnderMouse();

            // 二쇰Ц???寃잜똿 以묒씠嫄곕굹 諛⑷툑 ?寃잜똿 ?대┃??泥섎━???꾨젅?꾩씠硫?
            // 媛숈? ?대┃?쇰줈 ?몃깽 ?꾩씠?쒖쓣 吏묐뒗 ?쇰컲 ?낅젰???댁뼱吏吏 ?딄쾶 留됱뒿?덈떎.
            if (targetingController != null && targetingController.BlocksPointerInput)
            {
                _inventoryHighlight.Show(false);
                return;
            }

            if (_isPointerInputBlocked)
            {
                _inventoryHighlight.Show(false);
                return;
            }

            if (Input.GetMouseButtonDown(0))
                HandleLeftClick();

            if (_selectedGrid == null)
            {
                _inventoryHighlight.Show(false);
                return;
            }

            UpdateHighlight();
        }

        public bool TryAddItem(ItemInstance itemInstance, int amount = 1)
        {
            if (_selectedGrid == null || _selectedGrid.AcceptOnlyOneByOneItems)
                SelectedGrid = ResolveItemGrid();

            if (_selectedGrid == null || itemPrefab == null || canvasTransform == null || itemInstance == null || itemInstance.data == null)
            {
                Debug.LogError("InventoryController: ?몃깽?좊━ 洹몃━?? ?꾩씠???꾨━?? Canvas, ItemData ?곌껐???뺤씤?댁빞 ?⑸땲??");
                return false;
            }

            // YJW 履쎌? "?대뼡 ?꾩씠?쒖씤媛"瑜?ItemInstance濡??섍린怨? NYH ?몃깽?좊━媛 "紐?媛쒕줈 ?볦쓣 寃껋씤媛"瑜?寃곗젙?⑸땲??
            // 洹몃옒???ㅽ깮 ?섎웾? ItemInstance媛 ?꾨땲??InventoryItem.Count????ν빀?덈떎.
            // amount瑜??댁뼱???댁쑀???섏쨷??蹂댁긽/?곸옄泥섎읆 ??踰덉뿉 ?щ윭 媛쒕? ?ｋ뒗 ?먮쫫??媛숈? API濡?諛쏄린 ?꾪빐?쒖엯?덈떎.
            amount = Mathf.Max(1, amount);

            // ?ъ뀡/?ㅽ겕濡ㅼ? ??UI瑜?留뚮뱾湲??꾩뿉 湲곗〈 ?ㅽ깮??癒쇱? ?⑹묩?덈떎.
            // ?⑹퀜議뚮떎硫???InventoryItem GameObject瑜?留뚮뱾 ?꾩슂媛 ?놁쑝誘濡??ш린???앸깄?덈떎.
            if (TryMergeStack(itemInstance, amount))
                return true;

            InventoryItem item = CreateInventoryItem(itemInstance);

            // ???щ’??留뚮뱾 ?뚮룄 ?섎웾? ItemInstance???곗? ?딄퀬 InventoryItem?먮쭔 湲곕줉?⑸땲??
            // ?대젃寃??댁빞 媛숈? ItemInstance 援ъ“瑜??λ퉬/臾닿린/?ㅽ겕濡??ъ뀡??怨듭쑀?대룄 ?섎웾 梨낆엫???욎씠吏 ?딆뒿?덈떎.
            item.SetCount(amount);

            // ?먮룞 ?띾뱷 ?쒖뿉??醫뚯긽?⑤????묒뼱???ㅼ뼱媛????덈뒗 泥?鍮?怨듦컙???ｌ뒿?덈떎.
            Vector2Int? pos = _selectedGrid.FindSpaceForItem(item);
            if (pos == null)
            {
                Destroy(item.gameObject);
                return false;
            }

            _selectedGrid.PlaceItemInternal(item, pos.Value.x, pos.Value.y);
            _items.Add(item);
            OnItemAdded?.Invoke(item);
            RaiseInventoryChanged();
            return true;
        }

        public bool TryRemoveItem(InventoryItem item)
        {
            if (item == null) return false;

            ItemGrid targetGrid = _selectedGrid != null ? _selectedGrid : ResolveItemGrid();
            if (targetGrid == null)
            {
                Debug.LogError("InventoryController: ?꾩씠?쒖쓣 ?쒓굅???몃깽?좊━ 洹몃━?쒕? 李얠쓣 ???놁뒿?덈떎.");
                return false;
            }

            // 吏湲??먯뿉 ?ㅺ퀬 ?덈뜕 ?꾩씠?쒖쓣 ?몃? ?쒖뒪?쒖씠 ?쒓굅??寃쎌슦, ?쒕옒洹??곹깭??媛숈씠 ?뺣━?⑸땲??
            if (_selectedItem == item)
            {
                _selectedItem = null;
                _dragRect = null;
                ClearDragStartPosition();
            }

            _items.Remove(item);
            targetGrid.RemoveItem(item);
            RaiseInventoryChanged();
            return true;
        }

        public void ForgetItem(InventoryItem item)
        {
            if (item == null) return;

            if (_items.Remove(item))
                RaiseInventoryChanged();
        }

        public bool TryConsumeSelectedItem(InventoryItem expected)
        {
            return TryConsumeHeldItem(expected, false);
        }

        public bool TryConsumeOneItem(InventoryItem item)
        {
            if (item == null || item.ItemInstance == null)
                return false;

            // ?ㅽ깮??2媛??댁긽?대㈃ ?꾩씠??UI??洹몃?濡??먭퀬 ?섎웾留?1 媛먯냼?쒗궢?덈떎.
            // 留덉?留?1媛쒕? ???뚮쭔 湲곗〈 ??젣 ?먮쫫?쇰줈 ?섍꺼 GameObject? 洹몃━???먯쑀瑜??뺣━?⑸땲??
            if (item.Count > 1)
            {
                item.TryDecreaseCount(1);

                // ?붾툝?대┃ ?ъ슜 吏곹썑?먮뒗 ?꾩씠?쒖씠 ?먯뿉 ?ㅻ젮 ?덉쓣 ???덉쑝誘濡?
                // ?섎웾留?以꾩씤 ?ㅼ뿉???먮옒 移몄쑝濡??섎룎???쒕옒洹??곹깭瑜??앸깄?덈떎.
                if (_selectedItem == item)
                    CancelDragAndReturnItem();

                RaiseInventoryChanged();
                return true;
            }

            if (_selectedItem == item)
                return TryConsumeHeldItem(item, true);

            return TryRemoveItem(item);
        }

        /// <summary>
        /// ?꾩옱 留덉슦?ㅼ뿉 ?ㅺ퀬 ?덈뒗 ?꾩씠?쒖쓣 ?몃깽?좊━ ?뚯쑀 紐⑸줉怨??쒕옒洹??곹깭?먯꽌 ?쒓굅?⑸땲??
        /// ?λ퉬 ?μ갑泥섎읆 GameObject瑜?蹂닿??댁빞 ?섎뒗 寃쎌슦 destroyObject=false, ?ъ뀡/?ㅽ겕濡??뚮퉬泥섎읆 ?щ씪?몄빞 ?섎뒗 寃쎌슦 true瑜??ъ슜?⑸땲??
        /// </summary>
        public bool TryConsumeHeldItem(InventoryItem expected, bool destroyObject)
        {
            // ?먯뿉 ???꾩씠?쒓낵 ?붿껌 ?꾩씠?쒖씠 ?ㅻⅤ硫??ㅻⅨ ?쒖뒪?쒖씠 ?섎せ???꾩씠?쒖쓣 ?뚮퉬?섎젮??寃껋씠誘濡??ㅽ뙣?⑸땲??
            if (expected == null || _selectedItem != expected)
                return false;

            _items.Remove(expected);
            expected.SetRaycastTarget(false);
            _selectedItem = null;
            _overlapItem = null;
            _dragRect = null;
            _lastPickedItem = null;
            ClearDragStartPosition();
            _inventoryHighlight.Show(false);
            if (destroyObject)
                Destroy(expected.gameObject);

            RaiseInventoryChanged();
            return true;
        }

        public void HoldItem(InventoryItem item, ItemGrid returnGrid, int returnX, int returnY)
        {
            if (item == null) return;

            if (_selectedItem != null)
            {
                Debug.LogWarning("InventoryController: ?대? ?먯뿉 ???꾩씠?쒖씠 ?덉뼱 ???꾩씠?쒖쓣 ?????놁뒿?덈떎.");
                return;
            }

            if (!_items.Contains(item))
                _items.Add(item);

            item.gameObject.SetActive(true);
            item.SetRaycastTarget(false);
            _selectedItem = item;
            _overlapItem = null;
            _dragRect = item.GetComponent<RectTransform>();

            if (_dragRect != null)
            {
                _dragRect.SetParent(canvasTransform, true);
                _dragRect.position = Input.mousePosition;
                BringDraggedItemToFront();
            }

            _dragStartGrid = returnGrid;
            _dragStartX = returnX;
            _dragStartY = returnY;
            RaiseInventoryChanged();
        }

        public bool CanPlaceInDefaultGrid(InventoryItem item)
        {
            if (item == null) return false;

            // ?먮룞 ?μ갑 援먯껜泥섎읆 "?ㅼ젣濡??ｊ린 ????怨듦컙留??뺤씤?댁빞 ?섎뒗 ?먮쫫?먯꽌 ?ъ슜?⑸땲??
            ItemGrid targetGrid = ResolveItemGrid();
            return targetGrid != null && targetGrid.FindSpaceForItem(item).HasValue;
        }

        /// <summary>
        /// ?꾩씠?쒖쓣 湲곕낯 ?몃깽?좊━ 洹몃━?쒖쓽 泥?鍮?怨듦컙???먮룞 諛곗튂?⑸땲??
        /// ?λ퉬 援먯껜 ??湲곗〈 ?λ퉬瑜?媛諛⑹쑝濡??뚮젮蹂대궪 ???ъ슜?⑸땲??
        /// </summary>
        public bool TryPlaceInDefaultGrid(InventoryItem item)
        {
            if (item == null) return false;

            ItemGrid targetGrid = ResolveItemGrid();
            if (targetGrid == null) return false;

            // ?ㅼ젣 諛곗튂 ?꾩뿉 FindSpaceForItem?쇰줈 醫뚯긽??鍮덉뭏??李얠뒿?덈떎.
            Vector2Int? pos = targetGrid.FindSpaceForItem(item);
            if (pos == null) return false;

            // ?μ갑 以??④꺼???InventoryItem???ㅼ떆 蹂댁씠寃?耳???洹몃━???꾨옒濡?諛곗튂?⑸땲??
            item.gameObject.SetActive(true);
            targetGrid.PlaceItemInternal(item, pos.Value.x, pos.Value.y);
            item.SetRaycastTarget(true);
            if (!_items.Contains(item))
                _items.Add(item);

            RaiseInventoryChanged();
            return true;
        }

        public InventoryItem CreateItemView(ItemInstance itemInstance, Transform parent, bool playerOwned)
        {
            if (itemPrefab == null || itemInstance == null || itemInstance.data == null || parent == null)
            {
                Debug.LogError("InventoryController: ?꾩씠??UI 酉곕? 留뚮뱾湲??꾪븳 Prefab, ItemData, Parent ?곌껐???뺤씤?댁빞 ?⑸땲??");
                return null;
            }   

            InventoryItem item = Instantiate(itemPrefab, parent);

            // playerOwned媛 false??酉곕뒗 ?곸젏/蹂댁긽 誘몃━蹂닿린泥섎읆 ?몃깽?좊━ ?뚯쑀臾쇱씠 ?꾨땶 ?쒖떆?⑹쑝濡??????덉뒿?덈떎.
            item.Set(itemInstance, playerOwned ? this : null);
            return item;
        }

        /// <summary>
        /// ?몃깽?좊━ ?⑤꼸???대┝ ?곹깭瑜??몃? ?좉? 而댄룷?뚰듃媛 ?뚮젮以????ъ슜?⑸땲??
        /// ?ロ엳???쒓컙?먮뒗 ?ㅺ퀬 ?덈뜕 ?꾩씠?쒖쓣 ?먮옒 移몄쑝濡??섎룎由ш퀬, ?댄썑 ?대┃ ?낅젰??李⑤떒?⑸땲??
        /// </summary>
        public void SetInventoryOpen(bool isOpen)
        {
            _isInventoryOpen = isOpen;
            SetPointerInputBlocked(!isOpen);
        }

        /// <summary>
        /// ?몃깽?좊━ ?대┃ ?낅젰??留됯굅???ㅼ떆 ?덉슜?⑸땲??
        /// ?낅젰??留됱쓣 ?뚮뒗 ?꾩옱 ?쒕옒洹?以묒씤 ?꾩씠?쒖씠 ?щ씪吏吏 ?딅룄濡?癒쇱? ?먯쐞移섎줈 ?섎룎由쎈땲??
        /// </summary>
        public void SetPointerInputBlocked(bool value)
        {
            _isPointerInputBlocked = value;
            if (!value) return;

            // ?⑥닚??_selectedItem = null留??섎㈃ 洹몃━?쒖뿉??鍮좎졇 ?덈뜕 ?꾩씠?쒖씠 ?뚯븘媛?怨녹쓣 ?껋뒿?덈떎.
            // 洹몃옒???낅젰??留됯린 ?꾩뿉 諛섎뱶???쒕옒洹?痍⑥냼 泥섎━瑜?癒쇱? ?ㅽ뻾?⑸땲??
            CancelDragAndReturnItem();

            _overlapItem = null;
            _inventoryHighlight.Show(false);
        }

        /// <summary>
        /// ?뚮젅?댁뼱 ?뚯쑀 ?몃깽?좊━???ｌ쓣 ?꾩씠??UI瑜?Canvas ?꾨옒???앹꽦?⑸땲??
        /// </summary>
        private InventoryItem CreateInventoryItem(ItemInstance itemInstance)
        {
            return CreateItemView(itemInstance, canvasTransform, true);
        }

        private bool TryMergeStack(ItemInstance itemInstance, int amount)
        {
            // ?대쾲 ?ㅽ깮 ?쒖뒪?쒖? YJW ItemBase瑜?嫄대뱶由ъ? ?딄린 ?꾪빐,
            // "?ㅽ깮 媛???щ?"瑜??몃깽?좊━ 履????寃?щ줈留?寃곗젙?⑸땲??
            if (!IsStackableItem(itemInstance.data))
                return false;

            foreach (InventoryItem item in _items)
            {
                if (item == null || item.ItemInstance == null)
                    continue;

                // 媛숈? ScriptableObject 李몄“硫?媛숈? ?ъ뀡/?ㅽ겕濡ㅻ줈 蹂닿퀬 ??移몄뿉 ?⑹묩?덈떎.
                if (item.ItemData != itemInstance.data)
                    continue;

                // 湲곗〈 ?щ’???⑹퀜吏??뚮룄 ItemInstance??嫄대뱶由ъ? ?딆뒿?덈떎.
                // ?ㅽ깮? 蹂닿? 諛⑹떇??臾몄젣?쇱꽌 InventoryItem.Count留?利앷??쒗궎??寃껋씠 梨낆엫 遺꾨━??留욎뒿?덈떎.
                item.AddCount(amount);
                RaiseInventoryChanged();
                return true;
            }

            return false;
        }

        private static bool IsStackableItem(ItemBase itemData)
        {
            if (itemData == null)
                return false;

            // ?λ퉬??媛뺥솕/?二?媛먯젙 媛숈? 媛쒕퀎 ?곹깭媛 ?덉쑝誘濡??⑹튂吏 ?딆뒿?덈떎.
            // ?꾩옱???ъ뀡怨??ㅽ겕濡ㅻ쭔 ?덉쟾?섍쾶 ?ㅽ깮 ??곸쑝濡?遊낅땲??
            return itemData is PotionData || itemData.ItemType == ItemType.Scroll;
        }

        private void ResolveUseControllers()
        {
            // ?ъ뿉 吏곸젒 遺숈뿬?먯? ?딆븘???붾툝?대┃ ?ъ슜 ?먮쫫???숈옉?섎룄濡?媛숈? ?ㅻ툕?앺듃???먮룞?쇰줈 遺숈엯?덈떎.
            if (targetingController == null)
                targetingController = GetComponent<InventoryTargetingController>();

            if (targetingController == null)
                targetingController = gameObject.AddComponent<InventoryTargetingController>();

            targetingController.Configure(this);

            // ?ъ슜 而⑦듃濡ㅻ윭?????吏??而⑦듃濡ㅻ윭瑜??꾩슂濡??섎?濡?targetingController 以鍮???珥덇린?뷀빀?덈떎.
            if (itemUseController == null)
                itemUseController = GetComponent<InventoryItemUseController>();

            if (itemUseController == null)
                itemUseController = gameObject.AddComponent<InventoryItemUseController>();

            itemUseController.Configure(this, targetingController);
        }

        /// <summary>
        /// ?꾩옱 ?ъ뿉???ъ슜??ItemGrid瑜?李얠뒿?덈떎.
        /// ?쒖꽦 ?ㅻ툕?앺듃?먯꽌 癒쇱? 李얘퀬, ?놁쑝硫?鍮꾪솢??UI ?ㅻ툕?앺듃源뚯? ?ы븿?댁꽌 ?ㅼ떆 ?먯깋?⑸땲??
        /// </summary>
        private ItemGrid ResolveItemGrid()
        {
            if (defaultInventoryGrid != null)
                return defaultInventoryGrid;

            ItemGrid activeGrid = FindFirstObjectByType<ItemGrid>();
            if (activeGrid != null && !activeGrid.AcceptOnlyOneByOneItems) return activeGrid;

            // ?몃깽?좊━ ?⑤꼸??鍮꾪솢?깊솕???곹깭?먯꽌???쒕엻 ?꾩씠???띾뱷? 媛?ν빐???섎?濡?
            // ?ъ뿉 議댁옱?섏?留?鍮꾪솢?깊솕??UI源뚯? ?ы븿?댁꽌 ??踰???李얠뒿?덈떎.
            ItemGrid[] allGrids = Resources.FindObjectsOfTypeAll<ItemGrid>();
            foreach (ItemGrid grid in allGrids)
            {
                if (grid == null || !grid.gameObject.scene.IsValid() || grid.AcceptOnlyOneByOneItems) continue;
                return grid;
            }

            return null;
        }

        public void RefreshSelectedGridUnderMouse()
        {
            ItemGrid hoveredGrid = FindGridUnderMouse();
            if (hoveredGrid != null)
                SelectedGrid = hoveredGrid;
        }

        private ItemGrid FindGridUnderMouse()
        {
            ItemGrid[] grids = Resources.FindObjectsOfTypeAll<ItemGrid>();
            ItemGrid fallback = null;

            foreach (ItemGrid grid in grids)
            {
                if (grid == null || !grid.gameObject.scene.IsValid() || !grid.gameObject.activeInHierarchy) continue;
                if (!grid.ContainsScreenPoint(Input.mousePosition)) continue;

                if (grid.AcceptOnlyOneByOneItems)
                    return grid;

                fallback = grid;
            }

            return fallback;
        }

        /// <summary>
        /// 留덉슦???쇱そ ?대┃ ?낅젰??泥섎━?⑸땲??
        /// ?먯뿉 ???꾩씠?쒖씠 ?놁쑝硫?吏묎퀬, ?대? ?ㅺ퀬 ?덉쑝硫??꾩옱 移몄뿉 諛곗튂瑜??쒕룄?⑸땲??
        /// ?ㅼ쭏?곸씤 寃뚯엫?먯꽌 ?붾툝?대┃???ъ슜???몄텧?섎뒗 ?쒖옉??
        /// </summary>
        private void HandleLeftClick()
        {
            // ?ㅺ퀬 ?덈뒗 ?꾩씠?쒖쓣 ?몃깽?좊━ ?⑤꼸 諛뽰뿉 ?볦쑝硫? 洹몃━??諛곗튂媛 ?꾨땲???뚮젅?댁뼱 諛쒕컩 ?쒕엻?쇰줈 ?댁꽍?⑸땲??
            if (_selectedItem != null && IsPointerOutsideInventoryPanel())
            {
                TryDropSelectedItemToGround();
                _lastPickedItem = null;
                return;
            }

            // ?쒕옒洹??곗꽑 諛⑹떇: 泥??대┃? 湲곗〈泥섎읆 吏묎퀬, 媛숈? ?꾩씠?쒖쓣 吏㏃? ?쒓컙 ?덉뿉 ?ㅼ떆 ?대┃?섎㈃ ?ъ슜?쇰줈 ?꾪솚?⑸땲??
            if (_selectedItem != null && IsDoubleClickOnSelectedItem())
            {
                itemUseController?.TryUseDoubleClickedItem(_selectedItem);
                _lastPickedItem = null;
                return;
            }

            if (_selectedGrid == null) return;

            Vector2Int gridPos = GetMouseGridPosition();

            // ?먯씠 鍮꾩뼱 ?덉쑝硫??대┃??移몄쓽 ?꾩씠?쒖쓣 吏묎퀬, ?ㅺ퀬 ?덉쑝硫??꾩옱 ?꾩튂???대젮?볦뒿?덈떎.
            if (_selectedItem == null) TryPickUpItem(gridPos);
            else TryPlaceSelectedItem(gridPos);
        }

        /// <summary>
        /// 吏?뺥븳 洹몃━??醫뚰몴???덈뒗 ?꾩씠?쒖쓣 吏묒뼱 留덉슦???쒕옒洹??곹깭濡??꾪솚?⑸땲??
        /// ?대┃??移몄씠 鍮꾩뼱 ?덉쑝硫??꾨Т ?쇰룄 ?섏? ?딆뒿?덈떎.
        /// </summary>
        private void TryPickUpItem(Vector2Int gridPos)
        {
            // PickUpItem()???몄텧?섎㈃ ?대떦 ?꾩씠?쒖씠 洹몃━???щ’ 諛곗뿴?먯꽌 鍮좎쭛?덈떎.
            // 洹몃옒???몄텧 ?꾩뿉 GetItemAt()?쇰줈 ?꾩씠?쒖쓣 癒쇱? 李얘퀬, ?먮옒 移??뺣낫瑜???ν빐???⑸땲??
            InventoryItem item = _selectedGrid.GetItemAt(gridPos.x, gridPos.y);
            if (item == null) return;

            SaveDragStartPosition(item);

            // ?ㅼ젣濡??꾩씠?쒖쓣 洹몃━?쒖뿉??鍮쇱꽌 ?먯뿉 ??땲??
            // ???몄텧? ??踰덈쭔 ?댁빞 ?⑸땲?? ??踰??몄텧?섎㈃ 泥??몄텧?먯꽌 ?대? 鍮좎졇????踰덉㎏??null???????덉뒿?덈떎.
            _selectedItem = _selectedGrid.PickUpItem(gridPos.x, gridPos.y);
            if (_selectedItem == null) return;

            _selectedItem.SetRaycastTarget(false);
            // ?붾툝?대┃ ?먯젙???꾪빐 "諛⑷툑 吏묒? ?꾩씠??怨?吏묒? ?쒓컙??湲곕줉?⑸땲??
            _lastPickedItem = _selectedItem;
            _lastPickTime = Time.unscaledTime;
            _dragRect = _selectedItem.GetComponent<RectTransform>();

            // ?쒕옒洹?以묒씤 ?꾩씠?쒖? Canvas 理쒖긽?⑥쑝濡??щ젮 ?ㅻⅨ ?щ’/?꾩씠???ㅼ뿉 媛?ㅼ?吏 ?딄쾶 ?⑸땲??
            _dragRect.SetParent(canvasTransform, true);
            BringDraggedItemToFront();
        }

        /// <summary>
        /// ?쒕옒洹몃? ?쒖옉???꾩씠?쒖쓽 ?먮옒 ?꾩튂瑜???ν빀?덈떎.
        /// ?섏쨷???몃깽?좊━媛 ?ロ엳嫄곕굹 ?쒕옒洹멸? 痍⑥냼?섎㈃ ???뺣낫濡??꾩씠?쒖쓣 ?쒖옄由щ줈 ?뚮젮蹂대깄?덈떎.
        /// </summary>
        private void SaveDragStartPosition(InventoryItem item)
        {
            _dragStartGrid = _selectedGrid;
            _dragStartX = item.OnGridPositionX;
            _dragStartY = item.OnGridPositionY;
        }

        /// <summary>
        /// ??ν빐???쒕옒洹??쒖옉 ?꾩튂瑜?鍮꾩썎?덈떎.
        /// ?꾩씠??諛곗튂媛 ?앸궗嫄곕굹 ?쒕옒洹?痍⑥냼 泥섎━媛 ?앸궗?????댁쟾 醫뚰몴媛 ?⑥? ?딄쾶 ?⑸땲??
        /// </summary>
        private void ClearDragStartPosition()
        {
            _dragStartGrid = null;
            _dragStartX = 0;
            _dragStartY = 0;
        }

        /// <summary>
        /// ?꾩옱 ?ㅺ퀬 ?덈뒗 ?꾩씠?쒖쓣 吏?뺥븳 洹몃━??醫뚰몴???대젮?볦쑝?ㅺ퀬 ?쒕룄?⑸땲??
        /// 諛곗튂 ?꾩튂???꾩씠???섎굹留?寃뱀튂硫??쒕줈 援먯껜?섎뒗 諛⑹떇?쇰줈 泥섎━?⑸땲??
        /// </summary>
        private void TryPlaceSelectedItem(Vector2Int gridPos)
        {
            InventoryItem placedItem = _selectedItem;
            bool placed = _selectedGrid.TryPlaceItem(_selectedItem, gridPos.x, gridPos.y, ref _overlapItem);
            if (!placed)
            {
                if (_selectedGrid.AcceptOnlyOneByOneItems)
                    CancelDragAndReturnItem();

                return;
            }

            placedItem?.SetRaycastTarget(true);
            _selectedItem = null;
            if (_overlapItem != null)
            {
                // 諛곗튂 ?꾩튂?????섎굹???꾩씠?쒕쭔 寃뱀낀?ㅻ㈃ ?쒕줈 援먯껜?⑸땲??
                // ???꾩씠?쒖? 洹몃━?쒖뿉 ?대젮?볤퀬, 寃뱀퀜 ?덈뜕 ?꾩씠?쒖쓣 ?먯뿉 ???곹깭濡??꾪솚?⑸땲??
                // ?대븣 _dragStartX/Y??洹몃?濡??〓땲?? 洹몃옒???몃깽?좊━瑜??レ쑝硫?寃뱀퀜 ?덈뜕 ?꾩씠?쒖씠
                // 諛⑷툑 鍮꾩썙吏?"泥섏쓬 ?ㅼ뿀???꾩씠?쒖쓽 ?먮━"濡??뚯븘媛????덉뒿?덈떎.
                _selectedItem = _overlapItem;
                _overlapItem = null;
                _selectedItem.SetRaycastTarget(false);
                _dragRect = _selectedItem.GetComponent<RectTransform>();
                _dragRect.SetParent(canvasTransform, true);
                BringDraggedItemToFront();
            }
            else
            {
                // ?꾩쟾??鍮?移몄뿉 ?대젮?볦븯?ㅻ㈃ ?쒕옒洹멸? ?앸궃 寃껋씠誘濡??먮옒 ?꾩튂 諛깆뾽? ???댁긽 ?꾩슂 ?놁뒿?덈떎.
                _dragRect = null;
                ClearDragStartPosition();
            }

            RaiseInventoryChanged();
        }

        private void TryDropSelectedItemToGround()
        {
            if (_selectedItem == null || _selectedItem.ItemInstance == null)
                return;

            // 諛붾떏 ?쒕엻? KDU/YJW ItemPickup ?꾨━?뱀쓣 ?ъ슜?섏?留?
            // "?대뒓 移몄뿉 紐?媛쒓? ?볦??붽?"??GroundItemController媛 愿由ы빀?덈떎.
            ResolveGroundItemController();
            if (groundItemController == null)
            {
                Debug.LogWarning("InventoryController: GroundItemController瑜?李얠? 紐삵빐 ?꾩씠?쒖쓣 踰꾨┫ ???놁뒿?덈떎.");
                return;
            }

            InventoryItem itemToDrop = _selectedItem;
            if (!groundItemController.DropItem(itemToDrop.ItemInstance, itemToDrop.Count))
            {
                Debug.LogWarning("InventoryController: 諛붾떏 ?꾩씠???꾨━?? ?뚮젅?댁뼱, GridSystem ?곌껐???뺤씤?댁빞 ?⑸땲??");
                return;
            }

            // ?쒕엻 ?깃났 ?꾩뿉留??몃깽?좊━ UI? ?뚯쑀 紐⑸줉?먯꽌 ?쒓굅?⑸땲?? ?ㅽ뙣?섎㈃ 怨꾩냽 ?먯뿉 ???곹깭瑜??좎??⑸땲??
            // ?λ퉬/媛뺥솕/?二??곹깭??ItemInstance瑜??대? 諛붾떏 ?쎌뾽???섍꼈?쇰?濡??ш린?쒕뒗 UI留??뺣━?⑸땲??
            TryConsumeHeldItem(itemToDrop, true);
        }

        /// <summary>
        /// ?꾩옱 留덉슦???붾㈃ 醫뚰몴瑜??좏깮???몃깽?좊━ 洹몃━?쒖쓽 移?醫뚰몴濡?蹂?섑빀?덈떎.
        /// ???꾩씠?쒖쓣 ?ㅺ퀬 ?덉쓣 ?뚮뒗 ?꾩씠?쒖쓽 醫뚯긽??移몄씠 湲곗????섎룄濡?醫뚰몴瑜?蹂댁젙?⑸땲??
        /// </summary>
        private Vector2Int GetMouseGridPosition()
        {
            Vector2 mousePos = Input.mousePosition;
            if (_selectedItem != null)
            {
                // ?꾩씠???ш린媛 2移??댁긽???뚮룄 留덉슦?ㅺ? ?꾩씠?쒖쓽 醫뚯긽??湲곗? 移몄쓣 媛由ы궎?꾨줉 蹂댁젙?⑸땲??
                mousePos.x -= (_selectedItem.ItemData.Width - 1) * ItemGrid.TileWidth / 2f;
                mousePos.y += (_selectedItem.ItemData.Height - 1) * ItemGrid.TileHeight / 2f;
            }

            return _selectedGrid.ScreenToGridPosition(mousePos);
        }

        private bool IsPointerOutsideInventoryPanel()
        {
            RectTransform panelRect = ResolveInventoryPanelRect();
            if (panelRect == null)
                return false;

            return !IsPointerInsideRect(panelRect);
        }

        /// <summary>
        /// ?쒕엻 ?먯젙 湲곗????섎뒗 ?몃깽?좊━ ?꾩껜 ?⑤꼸??李얠뒿?덈떎.
        /// ?몄뒪?숉꽣???곌껐?섏뼱 ?덉쑝硫?洹?媛믪쓣 ?곌퀬, ?놁쑝硫?湲곕낯 ?몃깽?좊━ 洹몃━?쒖쓽 遺紐?RectTransform???꾩떆 湲곗??쇰줈 ?곷땲??
        /// </summary>
        public RectTransform ResolveInventoryPanelRect()
        {
            if (inventoryPanelRect != null)
                return inventoryPanelRect;

            ItemGrid grid = defaultInventoryGrid != null ? defaultInventoryGrid : ResolveItemGrid();
            if (grid != null)
                inventoryPanelRect = grid.GetComponentInParent<RectTransform>();

            return inventoryPanelRect;
        }

        /// <summary>
        /// Canvas 紐⑤뱶媛 Overlay媛 ?꾨땲?대룄 媛숈? 諛⑹떇?쇰줈 留덉슦?ㅺ? Rect ?덉뿉 ?덈뒗吏 寃?ы빀?덈떎.
        /// ?몃깽?좊━ ?⑤꼸 諛??쒕엻 ?먯젙??UI 移대찓???ㅼ젙???곕씪 ?붾뱾由ъ? ?딄쾶 ?섍린 ?꾪븳 蹂댁젙?낅땲??
        /// </summary>
        private static bool IsPointerInsideRect(RectTransform rect)
        {
            if (rect == null)
                return false;

            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera eventCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

            return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, eventCamera);
        }

        /// <summary>
        /// ?쒕옒洹?以묒씤 ?꾩씠??UI媛 留??꾨젅??留덉슦???꾩튂瑜??곕씪?ㅻ룄濡?媛깆떊?⑸땲??
        /// </summary>
        private void DragSelectedItemIcon()
        {
            if (_selectedItem == null || _dragRect == null) return;

            _dragRect.position = Input.mousePosition;
            BringDraggedItemToFront();
        }

        private void BringDraggedItemToFront()
        {
            if (_dragRect == null) return;

            // ?섏씠?쇱씠???ㅻⅨ UI媛 sibling ?쒖꽌瑜?諛붽퓭???먯뿉 ???꾩씠?쒖? ??긽 媛???꾩뿉 蹂댁씠寃??⑸땲??
            _dragRect.SetAsLastSibling();
        }

        /// <summary>
        /// ?꾩옱 ?먯뿉 ?ㅺ퀬 ?덈뒗 ?꾩씠?쒖쓣 ?쒕옒洹??쒖옉 ?꾩튂濡??섎룎由ш퀬, ?쒕옒洹??곹깭瑜??뺣━?⑸땲??
        /// ?몃깽?좊━ ?⑤꼸???レ쓣 ????泥섎━瑜??섏? ?딆쑝硫??꾩씠??UI媛 Canvas ?꾨옒?????덇굅???щ’?먯꽌 鍮좎쭏 ???덉뒿?덈떎.
        /// </summary>
        public void CancelDragAndReturnItem()
        {
            if (_selectedItem != null)
            {
                bool returned = false;

                if (_dragStartGrid != null)
                {
                    _dragStartGrid.PlaceItemInternal(_selectedItem, _dragStartX, _dragStartY);
                    _selectedItem.SetRaycastTarget(true);
                    returned = true;
                }
                else
                {
                    returned = TryReturnSelectedItemToDefaultGrid();
                }

                if (!returned)
                {
                    Debug.LogWarning("InventoryController: ?ㅺ퀬 ?덈뒗 ?꾩씠?쒖쓣 ?섎룎由?鍮??몃깽?좊━ 移몄쓣 李얠? 紐삵뻽?듬땲??");
                    return;
                }

                RaiseInventoryChanged();
            }

            _selectedItem = null;
            _overlapItem = null;
            _dragRect = null;
            ClearDragStartPosition();
            _inventoryHighlight.Show(false);
        }

        private bool IsDoubleClickOnSelectedItem()
        {
            if (_selectedItem == null || _lastPickedItem != _selectedItem)
                return false;

            // Time.timeScale ?곹뼢??諛쏆? ?딅룄濡?unscaledTime 湲곗??쇰줈 ?먯젙?⑸땲??
            return Time.unscaledTime - _lastPickTime <= doubleClickMaxDelay;
        }

        public bool TryReturnSelectedItemToDefaultGrid(InventoryItem item)
        {
            if (item == null) return true;

            ItemGrid targetGrid = ResolveItemGrid();
            if (targetGrid == null) return false;

            Vector2Int? pos = targetGrid.FindSpaceForItem(item);
            if (pos == null) return false;

            targetGrid.PlaceItemInternal(item, pos.Value.x, pos.Value.y);
            item.SetRaycastTarget(true);
            if (!_items.Contains(item))
                _items.Add(item);

            return true;
        }
        private bool TryReturnSelectedItemToDefaultGrid()
        {
            if (_selectedItem == null) return true;

            ItemGrid targetGrid = ResolveItemGrid();
            if (targetGrid == null) return false;

            Vector2Int? pos = targetGrid.FindSpaceForItem(_selectedItem);
            if (pos == null) return false;

            targetGrid.PlaceItemInternal(_selectedItem, pos.Value.x, pos.Value.y);
            _selectedItem.SetRaycastTarget(true);
            if (!_items.Contains(_selectedItem))
                _items.Add(_selectedItem);

            return true;
        }

        /// <summary>
        /// ??而댄룷?뚰듃媛 爰쇱쭏 ???쒕옒洹??곹깭媛 ?⑥븘 ?덉쑝硫??덉쟾?섍쾶 ?먯쐞移섎줈 ?섎룎由쎈땲??
        /// ?⑤꼸 ?좉??먯꽌 SetInventoryOpen(false)瑜??몄텧?섎뒗 寃껋씠 二?寃쎈줈?닿퀬, ???⑥닔???덉쇅 ?곹솴 ?鍮꾩슜?낅땲??
        /// </summary>
        private void OnDisable()
        {
            CancelDragAndReturnItem();
        }

        /// <summary>
        /// 留덉슦???꾩튂? ?꾩옱 ?쒕옒洹??곹깭??留욎떠 ?몃깽?좊━ ?섏씠?쇱씠?몄쓽 ?쒖떆 ?щ?, ?ш린, ?꾩튂瑜?媛깆떊?⑸땲??
        /// 鍮덉넀???뚮뒗 留덉슦???꾨옒 ?꾩씠?쒖쓣, ?꾩씠?쒖쓣 ?ㅺ퀬 ?덉쓣 ?뚮뒗 諛곗튂???곸뿭??蹂댁뿬以띾땲??
        /// </summary>
        private void UpdateHighlight()
        {
            Vector2Int gridPos = GetMouseGridPosition();
            if (_selectedItem == null)
            {
                // 鍮덉넀???뚮뒗 留덉슦???꾨옒???덈뒗 ?꾩씠???꾩껜 ?ш린瑜??섏씠?쇱씠?명빀?덈떎.
                InventoryItem target = _selectedGrid.GetItemAt(gridPos.x, gridPos.y);
                if (target == null)
                {
                    _inventoryHighlight.Show(false);
                    return;
                }

                _inventoryHighlight.Show(true);
                _inventoryHighlight.UpdateSize(target);
                _inventoryHighlight.UpdatePosition(_selectedGrid.GetLocalPositionForItem(target, target.OnGridPositionX, target.OnGridPositionY));
            }
            else
            {
                // ?꾩씠?쒖쓣 ?ㅺ퀬 ?덉쓣 ?뚮뒗 ?꾩옱 ?꾩튂??諛곗튂 媛?ν븳 寃쎌슦?먮쭔 ?섏씠?쇱씠?몃? 蹂댁뿬以띾땲??
                bool valid = _selectedGrid.CanAcceptItem(_selectedItem)
                    && _selectedGrid.IsWithinBoundary(gridPos.x, gridPos.y, _selectedItem.ItemData.Width, _selectedItem.ItemData.Height);
                _inventoryHighlight.Show(valid);
                _inventoryHighlight.UpdateSize(_selectedItem);
                _inventoryHighlight.UpdatePosition(_selectedGrid.GetLocalPositionForItem(_selectedItem, gridPos.x, gridPos.y));
            }
        }

        /// <summary>
        /// ?몃깽?좊━ ?댁슜??諛붾뚯뿀?뚯쓣 ?몃? 援щ룆?먯뿉寃??뚮┰?덈떎.
        /// UI 媛깆떊, ?먮ℓ 紐⑸줉 媛깆떊, ?μ갑 ?곹깭 媛깆떊 媛숈? ?꾩쿂由ш? ???대깽?몃? 援щ룆?????덉뒿?덈떎.
        /// </summary>
        public void RaiseInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }

        private void ResolveGroundItemController()
        {
            if (groundItemController == null)
                groundItemController = FindFirstObjectByType<GroundItemController>();

            if (groundItemController == null)
                groundItemController = gameObject.AddComponent<GroundItemController>();

            // ?먮룞 ?앹꽦??寃쎌슦?먮룄 ?몃깽?좊━ 李몄“媛 諛섎뱶???ㅼ뼱媛??
            // 諛붾떏 ?꾩씠?쒖쓣 二쇱슱 ??媛숈? TryAddItem 洹쒖튃怨??ㅽ깮 洹쒖튃???ъ슜?????덉뒿?덈떎.
            groundItemController.Configure(this);
        }
    }
}

