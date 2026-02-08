using UnityEngine;

[CreateAssetMenu(fileName = "PotatoSpritesData", menuName = "Hot Potato/Potato Sprites Data")]
public class PotatoSpritesData : ScriptableObject
{
    [Header("Potato Sprites (Assign 4 unique sprites)")]
    public Sprite[] potatoSprites;
    
    public Sprite GetSprite(int index)
    {   
        return potatoSprites[index];
    }
}