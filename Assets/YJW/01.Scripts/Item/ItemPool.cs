using UnityEngine;

[CreateAssetMenu(fileName = "ItemPool", menuName = "Item/ItemPool")]
public class ItemPool : ScriptableObject
{
	public ItemBase[] items;
}