using System;

public class NFTCharacter
{
    public int NFTId { get; set; }
    public string OwnerWallet { get; set; }
    public string UniqueAttributes { get; set; } // JSON representation
    public DateTime MintedAt { get; set; }
}
