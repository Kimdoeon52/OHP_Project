using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
	[SerializeField] ItemPool itemPool;
	[SerializeField] CurseData[] allCurses;  // 저주 목록
	[SerializeField] GameObject itemPickupPrefab;

	public void SpawnItem(Vector3 pos)
	{
		// 랜덤 SO 뽑기
		ItemBase randomSO = itemPool.items[Random.Range(0, itemPool.items.Length)];

		ItemInstance instance = randomSO.CreateInstance();

		// 픽업 프리팹 스폰
		var pickup = Instantiate(itemPickupPrefab, pos, Quaternion.identity);
		pickup.GetComponent<ItemPickup>().item = instance;
	}

	private int RollEnhanceLevel()
	{
		float roll = Random.value;
		if (roll < 0.55f) return 0;
		if (roll < 0.85f) return 1;
		if (roll < 0.95f) return 2;
		return 3;
	}

	private CurseData RollCurse()
	{
		if (Random.value > 0.5f) return null; // 50% 확률로 저주 없음
		return allCurses[Random.Range(0, allCurses.Length)];
	}
}