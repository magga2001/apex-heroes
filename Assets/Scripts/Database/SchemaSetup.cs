//using System;
//using Npgsql; // Install the Npgsql library for PostgreSQL

//public class SchemaSetup
//{
//    private const string ConnectionString = "Host=localhost;Username=your_username;Password=your_password;Database=game_database";

//    public static void CreateSchema()
//    {
//        using (var connection = new NpgsqlConnection(ConnectionString))
//        {
//            connection.Open();

//            // Players Table
//            string createPlayersTable = @"
//                CREATE TABLE IF NOT EXISTS players (
//                    player_id SERIAL PRIMARY KEY,
//                    username VARCHAR(50) UNIQUE NOT NULL,
//                    email VARCHAR(100) UNIQUE NOT NULL,
//                    wallet_address VARCHAR(100),
//                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
//                    last_login TIMESTAMP
//                );";

//            // Player Stats Table
//            string createPlayerStatsTable = @"
//                CREATE TABLE IF NOT EXISTS player_stats (
//                    player_id INT PRIMARY KEY REFERENCES players(player_id) ON DELETE CASCADE,
//                    xp INT DEFAULT 0,
//                    coins INT DEFAULT 0,
//                    matches_played INT DEFAULT 0,
//                    matches_won INT DEFAULT 0
//                );";

//            // Base Characters Table
//            string createBaseCharactersTable = @"
//                CREATE TABLE IF NOT EXISTS base_characters (
//                    character_id SERIAL PRIMARY KEY,
//                    name VARCHAR(50) NOT NULL,
//                    rarity VARCHAR(20) CHECK (rarity IN ('Common', 'Rare', 'Epic', 'Legendary', 'Mythic')) NOT NULL,
//                    base_health INT NOT NULL,
//                    base_attack INT NOT NULL,
//                    base_defense INT NOT NULL
//                );";

//            // NFT Characters Table
//            string createNFTCharactersTable = @"
//                CREATE TABLE IF NOT EXISTS nft_characters (
//                    nft_id SERIAL PRIMARY KEY,
//                    owner_wallet VARCHAR(100) NOT NULL,
//                    unique_attributes JSONB NOT NULL,
//                    minted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
//                );";

//            // Player Characters Table
//            string createPlayerCharactersTable = @"
//                CREATE TABLE IF NOT EXISTS player_characters (
//                    player_character_id SERIAL PRIMARY KEY,
//                    player_id INT REFERENCES players(player_id) ON DELETE CASCADE,
//                    character_id INT,
//                    nft_id INT,
//                    is_active BOOLEAN DEFAULT FALSE,
//                    FOREIGN KEY (character_id) REFERENCES base_characters(character_id) ON DELETE CASCADE,
//                    FOREIGN KEY (nft_id) REFERENCES nft_characters(nft_id) ON DELETE CASCADE
//                );";

//            // Abilities Table
//            string createAbilitiesTable = @"
//                CREATE TABLE IF NOT EXISTS abilities (
//                    ability_id SERIAL PRIMARY KEY,
//                    name VARCHAR(50) NOT NULL,
//                    description TEXT,
//                    character_id INT REFERENCES base_characters(character_id) ON DELETE CASCADE
//                );";

//            // Player Abilities Table
//            string createPlayerAbilitiesTable = @"
//                CREATE TABLE IF NOT EXISTS player_abilities (
//                    player_ability_id SERIAL PRIMARY KEY,
//                    player_id INT REFERENCES players(player_id) ON DELETE CASCADE,
//                    ability_id INT REFERENCES abilities(ability_id) ON DELETE CASCADE,
//                    unlocked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
//                );";

//            // Execute the schema creation queries
//            ExecuteQuery(connection, createPlayersTable);
//            ExecuteQuery(connection, createPlayerStatsTable);
//            ExecuteQuery(connection, createBaseCharactersTable);
//            ExecuteQuery(connection, createNFTCharactersTable);
//            ExecuteQuery(connection, createPlayerCharactersTable);
//            ExecuteQuery(connection, createAbilitiesTable);
//            ExecuteQuery(connection, createPlayerAbilitiesTable);

//            Console.WriteLine("Database schema initialized.");
//        }
//    }

//    private static void ExecuteQuery(NpgsqlConnection connection, string query)
//    {
//        using (var command = new NpgsqlCommand(query, connection))
//        {
//            command.ExecuteNonQuery();
//        }
//    }
//}
