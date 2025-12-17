using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ToonFormat.Internal.Shared;

namespace ToonFormat.Internal.Encode
{
    internal class FoldResult
    {
        /// <summary>
        /// The folded key with dot-separated segments (e.g., "data.metadata.items")
        /// </summary>
        public required string FoldedKey { get; set; }

        /// <summary>
        /// The remainder value after folding:
        /// <list type="bullet">
        /// <item>`null` if the chain was fully folded to a leaf (primitive, array, or empty object)</item>
        /// <item>An object if the chain was partially folded (depth limit reached with nested tail)</item>
        /// </list>
        /// </summary>
        public JsonNode? Remainder { get; set; }

        /// <summary>
        /// The leaf value at the end of the folded chain.
        /// Used to avoid redundant traversal when encoding the folded value.
        /// </summary>
        public required JsonNode LeafValue { get; set; }

        /// <summary>
        /// The number of segments that were folded.
        /// Used to calculate remaining depth budget for nested encoding.
        /// </summary>
        public required int SegmentCount { get; set; }
    }

    internal class KeyChain
    {
        public required IReadOnlyCollection<string> Segments { get; set; }
        public JsonNode? Tail { get; set; }
        public required JsonNode LeafValue { get; set; }
    }

    internal static class Folding
    {
        public static FoldResult? TryFoldKeyChain(string key, JsonNode? value, IReadOnlyCollection<string> siblings, ResolvedEncodeOptions options, IReadOnlySet<string>? rootLiteralKeys = null,
            string? pathPrefix = null, int? flattenDepth = null)
        {
            // Only fold when safe mode is enabled
            if (options.KeyFolding != ToonKeyFolding.Safe)
                return null;

            // Can only fold objects
            if (!Normalize.IsJsonObject(value))
                return null;

            // Use provided flattenDepth or fall back to options default
            var effectiveFlattenDepth = flattenDepth ?? options.FlattenDepth;

            // Collect the chain of single-key objects
            var keyChain = CollectSingleKeyChain(key, value, effectiveFlattenDepth);

            var segments = keyChain.Segments;
            var tail = keyChain.Tail;
            var leafValue = keyChain.LeafValue;

            // Need at least 2 segments for folding to be worthwhile
            if (segments.Count < 2)
                return null;

            // Validate all segments are safe identifiers
            if (!segments.All(ValidationShared.IsIdentifierSegment))
                return null;

            // Build the folded key (relative to current nesting level)
            var foldedKey = BuildFoldedKey(segments);

            // Build the absolute path from root
            var absolutePath = pathPrefix != null ? $"{pathPrefix}{Constants.DOT}{foldedKey}" : foldedKey;

            // Check for collision with existing literal sibling keys (at current level)
            if (siblings.Contains(foldedKey))
                return null;

            // Check for collision with root-level literal dotted keys
            if (rootLiteralKeys != null && rootLiteralKeys.Contains(absolutePath))
                return null;

            return new FoldResult
            {
                FoldedKey = foldedKey,
                Remainder = tail,
                LeafValue = leafValue,
                SegmentCount = segments.Count
            };
        }

        private static KeyChain CollectSingleKeyChain(string startKey, JsonNode? startValue, int maxDepth)
        {
            List<string> segments = [startKey];
            var currentValue = startValue;

            // Traverse nested single-key objects, collecting each key into segments array
            // Stop when we encounter: multi-key object, array, primitive, or depth limit
            while (segments.Count < maxDepth)
            {
                // must be an object to continue
                if (!Normalize.IsJsonObject(currentValue))
                    break;

                var jsonObject = currentValue?.AsObject();
                var keys = (jsonObject as IDictionary<string, JsonNode>)!.Keys;

                // must have exactly one key to continue the chain
                if (keys == null || keys.Count != 1)
                    break;

                var nextKey = keys.ElementAt(0);
                var nextValue = jsonObject[nextKey];

                segments.Add(nextKey);
                currentValue = nextValue;
            }

            // determine the tail
            if (!Normalize.IsJsonObject(currentValue) || Normalize.IsEmptyObject(currentValue))
            {
                // Array, primitive, null, or empty object - this is a leaf value
                return new KeyChain
                {
                    Segments =
                    segments,
                    Tail = null,
                    LeafValue = currentValue!
                };
            }

            // has keys -return as tail (remainder)
            return new KeyChain
            {
                Segments = segments,
                Tail = currentValue,
                LeafValue = currentValue!
            };
        }

        private static string BuildFoldedKey(IReadOnlyCollection<string> segments)
        {
            return string.Join(Constants.DOT, segments);
        }
    }
}
