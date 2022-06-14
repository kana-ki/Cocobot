using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cocobot.Utils
{
    static class MessageStringUtils
    {

        public static string StripMentions(this string message)
        {
            var regex = new Regex(@"<@!?[0-9]+>");
            return regex.Replace(message, string.Empty).Trim();
        }

        public static string NounCase(this string message) =>
            message switch
            {
                null or "" => message,
                _ => string.Concat(message[0].ToString().ToUpper(), message.ToLower().AsSpan(1))
            };

        public static List<string> AsListOfParagraphs(this string message)
        {
            return message.Split('\n').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }

        public static bool IndicatesConfirmation(this string message)
        {
            var regex = new Regex(@"\bye+(a+h?|s)?\b");
            if (regex.IsMatch(message))
                return true;

            var yesPhrases = new[] { "yes", "yeah", "ye", "ye!", "yup", "mhm", "mhm!", "true", "definitely", "absolutely", "yeees" };
            return AnyWordMatchesAnyPhrase(message, yesPhrases);
        }


        public static bool IndicatesContradiction(this string message)
        {
            var regex = new Regex(@"\bn(o+(p+e+)?|a+h+)\b");
            if (regex.IsMatch(message))
                return true;

            var yesPhrases = new[] { "no", "nope", "nah", "no!", "false" };
            return AnyWordMatchesAnyPhrase(message, yesPhrases);
        }

        public static bool MatchesAnyRegex(this string message, params Regex[] phrases)
        {
            foreach (var regex in phrases)
                if (regex.IsMatch(message))
                    return true;
            return false;
        }

        public static bool MatchesAnyRegex(this string message, params string [] phrases)
        {
            foreach (var regex in phrases)
                if (new Regex("\\b" + regex + "\\b", RegexOptions.IgnoreCase).IsMatch(message))
                    return true;
            return false;
        }

        public static bool MatchesAnyPhrase(this string message, params string[] phrases)
        {
            message = message.ToLower();
            var similarityComparer = new F23.StringSimilarity.NormalizedLevenshtein();
            foreach (var s in phrases)
            {
                var similarity = similarityComparer.Similarity(s, message);
                if (similarity > .7)
                    return true;
            }
            return false;
        }

        public static bool AnyWordMatchesAnyPhrase(this string message, params string[] phrases)
        {
            message = message.ToLower();
            foreach (var word in message.Split(' '))
                if (MatchesAnyPhrase(word, phrases))
                    return true;

            return false;
        }

        public static (string Phrase, double Score) IsSimilarToAnyPhrase(this string message, params string[] phrases) =>
            IsSimilarToAnyPhrase(message, (IEnumerable<string>) phrases);

        public static (string Phrase, double Score) IsSimilarToAnyPhrase(this string message, IEnumerable<string> phrases) {
            (string Phrase, double Score) highest = (null, 0);
            message = message.ToLower();
            var similarityComparer = new F23.StringSimilarity.NormalizedLevenshtein();
            foreach (var s in phrases)
            {
                var similarity = similarityComparer.Similarity(s, message);
                if (similarity > highest.Score)
                    highest = (s, similarity);
            }
            return highest;
        }

        public static (string Phrase, double Score) AnyWorldIsSimilarToAnyPhrase(this string message, params string[] phrases) =>
            AnyWorldIsSimilarToAnyPhrase(message, (IEnumerable<string>) phrases);

        public static (string Phrase, double Score) AnyWorldIsSimilarToAnyPhrase(this string message, IEnumerable<string> phrases)
        {
            (string Phrase, double Score) highest = (null, 0);
            message = message.ToLower();
            foreach (var word in message.Split(' '))
            {
                var similarity = IsSimilarToAnyPhrase(word, phrases);
                if (similarity.Score > highest.Score)
                    highest = similarity;
            }

            return highest;
        }

    }
}
