using System;
using System.Collections.Generic;

public class AbilityRepository
{
    public static List<AbilityData> GetAbilitiesForCharacter(int characterId)
    {
        string query = $"SELECT * FROM abilities WHERE character_id = {characterId};";
        var table = DatabaseManager.FetchData(query);

        List<AbilityData> abilities = new List<AbilityData>();
        foreach (System.Data.DataRow row in table.Rows)
        {
            abilities.Add(new AbilityData
            {
                AbilityId = Convert.ToInt32(row["ability_id"]),
                Name = row["name"].ToString(),
                Description = row["description"].ToString(),
                CharacterId = Convert.ToInt32(row["character_id"])
            });
        }
        return abilities;
    }

    public static void AddAbility(AbilityData ability)
    {
        string query = $@"
            INSERT INTO abilities (name, description, character_id)
            VALUES ('{ability.Name}', '{ability.Description}', {ability.CharacterId});
        ";
        DatabaseManager.ExecuteQuery(query);
    }
}
