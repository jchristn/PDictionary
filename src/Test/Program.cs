namespace Test
{
    using System;
    using System.Collections.Generic;
    using GetSomeInput;
    using PersistentDictionary;

    class Program
    {
        private static readonly string[] _Words = new string[]
        {
            "apple", "banana", "cherry", "date", "elderberry", "fig", "grape", "honeydew",
            "kiwi", "lemon", "mango", "nectarine", "orange", "papaya", "quince", "raspberry",
            "strawberry", "tangerine", "ugli", "vanilla", "watermelon", "xigua", "yuzu", "zucchini",
            "artichoke", "broccoli", "carrot", "daikon", "eggplant", "fennel", "garlic", "horseradish",
            "iceberg", "jalapeno", "kale", "leek", "mushroom", "napa", "onion", "pepper",
            "quinoa", "radish", "spinach", "tomato", "ube", "vegetable", "wasabi", "yam",
            "almond", "beet", "celery", "dill", "endive", "fava", "ginger", "hazelnut",
            "ice", "jackfruit", "kohlrabi", "lime", "mint", "nutmeg", "okra", "parsley",
            "rhubarb", "saffron", "thyme", "umami", "vanilla", "walnut", "xylitol", "yeast",
            "anise", "basil", "cilantro", "dill", "escarole", "fennel", "ginseng", "hyssop",
            "iris", "juniper", "kelp", "lavender", "marjoram", "nettle", "oregano", "peppermint",
            "rosemary", "sage", "tarragon", "uva", "verbena", "wintergreen", "yarrow", "zedoary"
        };

        private static readonly Random _Random = new Random();

        private static string GetRandomWord()
        {
            return _Words[_Random.Next(_Words.Length)];
        }

        private static Dictionary<string, string> GenerateRandomDictionary(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentException("Count must be greater than zero.", nameof(count));
            }

            var result = new Dictionary<string, string>();

            while (result.Count < count)
            {
                string key = GetRandomWord();
                string value = GetRandomWord();

                if (!result.ContainsKey(key))
                {
                    result.Add(key, value);
                }
            }

            return result;
        }

        static void Main(string[] args)
        {
            PDictionary<string, string> pdict = new PDictionary<string, string>("pdict.json");

            Console.WriteLine("");
            Console.WriteLine("Existing directory contents:");
            foreach (KeyValuePair<string, string> kvp in pdict)
                Console.WriteLine("| " + kvp.Key + ": " + kvp.Value);

            Console.WriteLine("");
            Console.WriteLine("Adding values");
            for (int i = 0; i < 10; i++)
                pdict.Add(GetRandomWord(), GetRandomWord());

            Console.WriteLine("");
            Console.WriteLine("Existing directory contents:");
            foreach (KeyValuePair<string, string> kvp in pdict)
                Console.WriteLine("| " + kvp.Key + ": " + kvp.Value);
            
            Console.WriteLine("");
            if (Inputty.GetBoolean("Empty", true))
                pdict.Clear();

            Console.WriteLine("");
        }
    }
}
