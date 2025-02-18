using System;
using System.Collections.Generic;

public class CharacterRepository
{
    public static List<BaseCharacter> GetBaseCharacters()
    {
        string query = "SELECT * FROM base_characters;";
        var table = DatabaseManager.FetchData(query);

        List<BaseCharacter> characters = new List<BaseCharacter>();
        foreach (System.Data.DataRow row in table.Rows)
        {
            characters.Add(new BaseCharacter
            {
                CharacterId = Convert.ToInt32(row["character_id"]),
                Name = row["name"].ToString(),
                Rarity = row["rarity"].ToString(),
                BaseHealth = Convert.ToInt32(row["base_health"]),
                BaseAttack = Convert.ToInt32(row["base_attack"]),
                BaseDefense = Convert.ToInt32(row["base_defense"])
            });
        }
        return characters;
    }

    public static NFTCharacter GetNFTCharacter(int nftId)
    {
        string query = $"SELECT * FROM nft_characters WHERE nft_id = {nftId};";
        var table = DatabaseManager.FetchData(query);

        if (table.Rows.Count > 0)
        {
            var row = table.Rows[0];
            return new NFTCharacter
            {
                NFTId = Convert.ToInt32(row["nft_id"]),
                OwnerWallet = row["owner_wallet"].ToString(),
                UniqueAttributes = row["unique_attributes"].ToString(),
                MintedAt = Convert.ToDateTime(row["minted_at"])
            };
        }

        return null; // NFT not found
    }
}
