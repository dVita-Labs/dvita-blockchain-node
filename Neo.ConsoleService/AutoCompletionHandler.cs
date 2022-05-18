using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.ConsoleService
{
    internal sealed class AutoCompletionHandler : IAutoCompleteHandler
    {
        public char[] Separators { get; set; } = new char[] { ' ', '.', '/', '\\' };

        // There are 3 levels atm: <1. command verb> <2. second level verb> <oprional 3rd level verb>, e.g:
        // create wallet
        //      or
        // get next validators
        private static Dictionary<string, List<List<string>>> AutocompletionDict = new();

        public static void RegisterSuggestions(string[] verbs) {
            var toplevel = verbs.First();
            var rest = verbs.Skip(1).ToList();

            var exists = AutocompletionDict.TryGetValue(toplevel, out var existent);

            if (exists && existent is not null) {
                // Create new 'tail' branch with an existent top-level verb
                existent.Add(rest);
            }
            else
            {
                // Create main branch with nonexistent top-level verb
                var newCollection = new [] { rest };
                AutocompletionDict.Add(toplevel, newCollection.ToList());
            }
        }

        public string[] GetSuggestions(string text, int index)
        {
            var tokens = text.Split(Separators.First(), StringSplitOptions.TrimEntries);

            if (tokens is not null && tokens.Any())
            {
                var level = tokens.Length;
                var toplevel = AutocompletionDict.Where(kv => kv.Key.StartsWith(tokens[0]));

                if (level == 1)
                {
                    return toplevel.Select(kv => kv.Key).ToArray();
                }
                else if (level > 1 && level <= 3 && toplevel.Count() == 1)
                {
                    var toplevelVerb = toplevel.First().Key;
                    var toplevelList = toplevel.First().Value;
                    if (tokens[0] == toplevelVerb)
                    {
                        var innerVerbs =
                            level == 2 ?
                                toplevelList
                                    .Select(l => String.Join(' ', l))
                                    .Where(s => s.StartsWith(tokens[1])) :
                            level == 3 ?
                                toplevelList
                                    .Where(l => l.Contains(tokens[1]))
                                    .SelectMany(l => l)
                                    .Skip(1)
                                    .Where(s => s.StartsWith(tokens[2])) :
                                null;
                        return innerVerbs.ToArray();
                    }
                }
            }
            
            return null;
        }
    }
}
