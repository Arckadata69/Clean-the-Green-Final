using UnityEngine;

[CreateAssetMenu(fileName = "NewAlmanacItem", menuName = "Almanac/Item Data")]
public class AlmanacItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemId = "item_01";
    public string itemName = "Item Name";
    [TextArea(2, 4)]
    public string description = "Item details go here.";

    [Header("Visuals")]
    public Sprite cardArtwork;

    [Header("Stats")]
    public string location = "Forest";
    public int value = 10;

    public string PickCountKey => $"Almanac_PickCount_{itemId}";

    public int GetPickCount()
    {
        return PlayerPrefs.GetInt(PickCountKey, 0);
    }

    public void AddPickCount(int amount = 1)
    {
        int next = Mathf.Max(0, GetPickCount() + amount);
        PlayerPrefs.SetInt(PickCountKey, next);
        PlayerPrefs.Save();
    }
}